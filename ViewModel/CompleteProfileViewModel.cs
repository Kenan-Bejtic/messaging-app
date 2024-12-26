using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using LoginWithFirebase.Helpers; 
using LoginWithFirebase.Services;
using Microsoft.Maui.Storage; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LoginWithFirebase.ViewModel
{
    public partial class CompleteProfileViewModel : BaseViewModel
    {
        private readonly FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;
        private readonly InviteCodeService _inviteCodeService;
        private readonly string _userId;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string gender;

        [ObservableProperty]
        private string inviteCode;

        public List<string> Genders { get; } = new List<string> { "Muški", "Ženski", "Ostalo" };

        
        private readonly string _defaultMaleProfileUrl = DefaultProfilePictures.MaleProfileUrl;
        private readonly string _defaultFemaleProfileUrl = DefaultProfilePictures.FemaleProfileUrl;

        public CompleteProfileViewModel(string userId)
        {
            _firebaseAuthClient = new FirebaseAuthClient(new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyB5dQbIgcUlyWq1w2D_pkIkq4JPPG9mpLo",
                AuthDomain = "razvoj-mobilnih-aplikacija.firebaseapp.com",
            });

            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _userId = userId;
            _inviteCodeService = new InviteCodeService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
        }

        [RelayCommand]
        public async Task CompleteProfile()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Username is required.",
                    "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Gender))
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    "Please select your gender.",
                    "OK");
                return;
            }

            try
            {
                
                var profilePictureUrl = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("profilePictureUrl")
                    .OnceSingleAsync<string>();

                
                if (string.IsNullOrWhiteSpace(profilePictureUrl))
                {
                    if (Gender == "Ženski")
                    {
                        profilePictureUrl = _defaultFemaleProfileUrl;
                    }
                    else
                    {
                        profilePictureUrl = _defaultMaleProfileUrl;
                    }
                }

              
                var userProfile = new
                {
                    username = Username,
                    gender = Gender,
                    profileCompleted = true,
                    profilePictureUrl = profilePictureUrl
                };

                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .PutAsync(userProfile);

               
                await _inviteCodeService.AssignInviteCodeAsync(_userId);

                
                var assignedInviteCode = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("inviteCode")
                    .OnceSingleAsync<string>();

                InviteCode = assignedInviteCode;

               
                Application.Current.MainPage =
                    new NavigationPage(new MainPage(_firebaseAuthClient));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"An error occurred: {ex.Message}",
                    "OK");
            }
        }

        [RelayCommand]
        private async Task UploadProfilePicture()
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Pick a profile picture"
                });

                if (result != null)
                {
                    using var stream = await result.OpenReadAsync();
                    var imagePath = result.FullPath;

                    var storage = new FirebaseStorage("razvoj-mobilnih-aplikacija.appspot.com");

                    var fileName = $"{_userId}_{Path.GetFileName(imagePath)}";
                    await storage
                        .Child("profile_pictures")
                        .Child(fileName)
                        .PutAsync(stream);

                    var downloadUrl = await storage
                        .Child("profile_pictures")
                        .Child(fileName)
                        .GetDownloadUrlAsync();

                    Console.WriteLine($"Download URL: {downloadUrl}");

                    if (!string.IsNullOrWhiteSpace(downloadUrl))
                    {
                        var profilePictureData = new { profilePictureUrl = downloadUrl };
                        await _firebaseClient
                            .Child("users")
                            .Child(_userId)
                            .PatchAsync(profilePictureData);

                        await Application.Current.MainPage.DisplayAlert(
                            "Success",
                            "Profile picture uploaded successfully.",
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"An error occurred: {ex.Message}",
                    "OK");
            }
        }
    }
}
