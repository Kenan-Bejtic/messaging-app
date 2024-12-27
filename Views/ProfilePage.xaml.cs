using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.ViewModel;
using LoginWithFirebase.Model;
using Firebase.Auth.Providers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Dispatching;
using System.ComponentModel;

namespace LoginWithFirebase.Views
{
    public partial class ProfilePage : ContentPage, INotifyPropertyChanged
    {
        private FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;

        private string _userId;

        private bool _hasPendingFriendRequests;
        public bool HasPendingFriendRequests
        {
            get => _hasPendingFriendRequests;
            set
            {
                if (_hasPendingFriendRequests != value)
                {
                    _hasPendingFriendRequests = value;
                    OnPropertyChanged(nameof(HasPendingFriendRequests));
                }
            }
        }

        public ProfilePage(FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            _firebaseAuthClient = firebaseAuthClient;

            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _userId = Preferences.Default.Get("UserId", string.Empty);

            if (string.IsNullOrWhiteSpace(_userId))
            {
                Console.WriteLine("[ERROR] User ID is empty. Cannot load user data.");
                DisplayAlert("Error", "User data not found.", "OK");
                return;
            }

            BindingContext = this;

           
        }

       
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserData();
        }

        private async void OnInviteFriendsClicked(object sender, EventArgs e)
        {
            try
            {
                var user = await _firebaseClient
                        .Child("users")
                        .Child(_userId)
                        .OnceSingleAsync<UserModel>();

                if (user != null)
                {
                    await Navigation.PushAsync(new FriendInvitationPage(user.InviteCode));
                }
                else
                {
                    await DisplayAlert("Error", "User data not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error navigating to FriendInvitationPage: {ex.Message}");
                await DisplayAlert("Error", "An error occurred while trying to invite friends.", "OK");
            }
        }

  
        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                if (_firebaseAuthClient == null)
                {
                    Console.WriteLine("Error: FirebaseAuthClient is null. Cannot sign out.");
                    await DisplayAlert("Error", "Unable to sign out. Please try again.", "OK");
                    return;
                }

                try
                {
                    var currentUser = _firebaseAuthClient.User;
                    if (currentUser != null)
                    {
                        _firebaseAuthClient.SignOut();
                        Console.WriteLine("[INFO] User signed out successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Warning: No authenticated user found, clearing local preferences.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during Firebase sign-out: " + ex.ToString());
                    await DisplayAlert("Error", "An error occurred while signing out.", "OK");
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
                    await DisplayAlert("Error", "An error occurred during logout.", "OK");
                    return;
                }

                var signInPage = new SignInPage(signInViewModel);
                if (signInPage == null)
                {
                    Console.WriteLine("Error: Failed to create SignInPage.");
                    await DisplayAlert("Error", "An error occurred during logout.", "OK");
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
                await DisplayAlert("Error", "An unexpected error occurred during logout.", "OK");
            }
        }

        
        private async Task LoadUserData()
        {
            try
            {
                var user = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .OnceSingleAsync<UserModel>();

                if (user != null)
                {
                    Dispatcher.Dispatch(() =>
                    {
                        string inviteCodeDisplay = !string.IsNullOrWhiteSpace(user.InviteCode)
                            ? $"#{user.InviteCode}"
                            : string.Empty;

                        
                        InviteCodeLabel.Text = $"Invite Code: {inviteCodeDisplay}";

                        
                        UsernameLabel.Text = user.Username ?? "Unknown";

                        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                        {
                            ProfileImage.Source = new UriImageSource
                            {
                                Uri = new Uri(user.ProfilePictureUrl),
                                CachingEnabled = true,
                                CacheValidity = TimeSpan.FromDays(1)
                            };
                        }
                        else
                        {
                            
                            ProfileImage.Source = "maleprofile.png"; 
                        }
                    });

                    
                    await CheckPendingFriendRequests(user.InviteCode);
                }
                else
                {
                    Console.WriteLine($"[ERROR] No user data found at 'users/{_userId}'.");
                    await DisplayAlert("Error", "User data not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error fetching user data: {ex.Message}");
                await DisplayAlert("Error",
                    $"An error occurred while fetching user data: {ex.Message}", "OK");
            }
        }

        
        private async Task CheckPendingFriendRequests(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                HasPendingFriendRequests = false;
                return;
            }

            try
            {
                var pendingRequests = await _firebaseClient
                    .Child("friend_requests")
                    .OrderBy("toInviteCode")
                    .EqualTo(inviteCode)
                    .OnceAsync<FriendRequest>();

                
                HasPendingFriendRequests = pendingRequests.Any(r =>
                    string.Equals(r.Object.Status, "pending", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error checking pending friend requests: {ex.Message}");
                HasPendingFriendRequests = false;
            }
        }

        
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
