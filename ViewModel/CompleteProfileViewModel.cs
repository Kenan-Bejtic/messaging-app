using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using LoginWithFirebase.Services;

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

        public List<string> Genders { get; } = new List<string> { "Male", "Female", "Other" };

        
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
            if (!string.IsNullOrWhiteSpace(Username))
            {
                try
                {
                    
                    var profilePictureUrl = await _firebaseClient
                        .Child("users")
                        .Child(_userId)
                        .Child("profilePictureUrl")
                        .OnceSingleAsync<string>();

                    
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
                    

                    
                    //await Application.Current.MainPage.DisplayAlert("Success", $"Profile completed! Your invite code is #{InviteCode}", "OK");
                    Application.Current.MainPage = new NavigationPage(new MainPage(_firebaseAuthClient));
                }
                catch (Exception ex)
                {
                   
                    await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Username is required.", "OK");
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
                    var uploadTask = await storage
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
                            .PutAsync(profilePictureData);

                        await Application.Current.MainPage.DisplayAlert("Success", "Profile picture uploaded successfully.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


    }

}












