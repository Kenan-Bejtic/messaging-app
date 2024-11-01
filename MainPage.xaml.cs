using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using LoginWithFirebase.ViewModel;
using Firebase.Auth.Providers;

namespace LoginWithFirebase
{
    public partial class MainPage : ContentPage
    {
        private  FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;
        private string _userId;
        public string inviteCode;

        public MainPage(FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            _firebaseAuthClient = firebaseAuthClient;

            
            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _userId = Preferences.Default.Get("UserId", string.Empty);

            if (string.IsNullOrWhiteSpace(_userId))
            {
                Console.WriteLine("[ERROR] User ID is empty. Cannot load user data.");
                return;
            }

           
            LoadUserData();
        }

        private int count = 0;

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                if (_firebaseAuthClient == null)
                {
                    Console.WriteLine("Error: FirebaseAuthClient is null. Cannot sign out.");
                    return;
                }

                
                try
                {
                    var currentUser = _firebaseAuthClient.User;
                    if (currentUser != null)
                    {
                        _firebaseAuthClient.SignOut();
                    }
                    else
                    {
                        Console.WriteLine("Warning: No authenticated user found, clearing local preferences.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during Firebase sign-out: " + ex.ToString());
                }

                
                Preferences.Default?.Remove("UserEmail");
                Preferences.Default?.Remove("UserId");

                
                var config = new FirebaseAuthConfig
                {
                    ApiKey = "AIzaSyB5dQbIgcUlyWq1w2D_pkIkq4JPPG9mpLo",
                    AuthDomain = "razvoj-mobilnih-aplikacija.firebaseapp.com",
                    Providers = new FirebaseAuthProvider[]
                    {
                new EmailProvider()
                    }
                };
                _firebaseAuthClient = new FirebaseAuthClient(config);

                
                if (Application.Current == null)
                {
                    Console.WriteLine("Error: Application.Current is null. Cannot navigate.");
                    return;
                }

                var signInViewModel = new SignInViewModel(_firebaseAuthClient);
                if (signInViewModel == null)
                {
                    Console.WriteLine("Error: Failed to create SignInViewModel.");
                    return;
                }

                var signInPage = new SignInPage(signInViewModel);
                if (signInPage == null)
                {
                    Console.WriteLine("Error: Failed to create SignInPage.");
                    return;
                }

                
                Dispatcher.Dispatch(async () =>
                {
                    await Task.Delay(50); 
                    Application.Current.MainPage = new NavigationPage(signInPage);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logout failed: " + ex.ToString());
            }
        }


        private async void FriendInvite(object sender, EventArgs e)
        {
            var friendInvitation = new FriendInvitationPage(inviteCode);

            
            await Navigation.PushAsync(friendInvitation);
        }











        private async Task LoadUserData()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_userId))
                {
                    Console.WriteLine($"[DEBUG] Loading data for user ID: {_userId}");


                    var user = await _firebaseClient
                        .Child("users")
                        .Child(_userId)
                        .OnceSingleAsync<UserModel>();

                    if (user != null)
                    {
                        Console.WriteLine($"[DEBUG] User data retrieved: Username={user.Username}, Gender={user.Gender}, ProfilePictureUrl={user.ProfilePictureUrl}, InviteCode={user.InviteCode}");
                        inviteCode = user.InviteCode;

                        Dispatcher.Dispatch(() =>
                        {

                            if (UsernameLabel != null)
                            {
                                
                                string inviteCodeDisplay = !string.IsNullOrWhiteSpace(user.InviteCode) ? $" #{user.InviteCode}" : "";
                                UsernameLabel.Text = $"{user.Username ?? "Unknown"}{inviteCodeDisplay}";
                                Console.WriteLine($"[DEBUG] UsernameLabel set to: {user.Username}{inviteCodeDisplay}");
                            }
                            else
                            {
                                Console.WriteLine("[ERROR] UsernameLabel is null");
                            }

                            if (GenderLabel != null)
                            {
                                GenderLabel.Text = user.Gender ?? "Unknown";
                                Console.WriteLine($"[DEBUG] GenderLabel set to: {user.Gender}");
                            }
                            else
                            {
                                Console.WriteLine("[ERROR] GenderLabel is null");
                            }

                            if (ProfileImage != null && !string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                            {
                                ProfileImage.Source = new UriImageSource
                                {
                                    Uri = new Uri(user.ProfilePictureUrl),
                                    CachingEnabled = true,
                                    CacheValidity = TimeSpan.FromDays(1)
                                };
                                Console.WriteLine($"[DEBUG] ProfileImage set to URL: {user.ProfilePictureUrl}");
                            }
                            else if (ProfileImage == null)
                            {
                                Console.WriteLine("[ERROR] ProfileImage is null");
                            }
                            else
                            {
                                Console.WriteLine("[DEBUG] No Profile Picture URL provided.");
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] User data is null");
                    }
                }
                else
                {
                    Console.WriteLine("[ERROR] User ID is null or empty");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error fetching user data: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred while fetching user data: {ex.Message}", "OK");
            }
        }



        public class UserModel
        {
            public string Username { get; set; }
            public string Gender { get; set; }
            public string ProfilePictureUrl { get; set; }
            public string InviteCode { get; set; }
        }
    }
}
