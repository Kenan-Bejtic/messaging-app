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
using LoginWithFirebase.Model;
using Firebase.Auth.Providers;
using System.Collections.ObjectModel;

namespace LoginWithFirebase
{
    public partial class MainPage : ContentPage
    {
        private  FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;
        private ObservableCollection<string> _friendList = new ObservableCollection<string>();
        private string _userId;
        public string inviteCode;

        public MainPage(FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            _firebaseAuthClient = firebaseAuthClient;

            
            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _userId = Preferences.Default.Get("UserId", string.Empty);
            FriendsCollectionView.ItemsSource = _friendList;
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
                    var user = await _firebaseClient
                        .Child("users")
                        .Child(_userId)
                        .OnceSingleAsync<UserModel>();

                    if (user != null)
                    {
                        inviteCode = user.InviteCode;

                        Dispatcher.Dispatch(() =>
                        {
                            string inviteCodeDisplay = !string.IsNullOrWhiteSpace(user.InviteCode)
                                ? $" #{user.InviteCode}"
                                : string.Empty;

                            UsernameLabel.Text = $"{user.Username ?? "Unknown"}{inviteCodeDisplay}";
                            GenderLabel.Text = user.Gender ?? "Unknown";

                            if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
                            {
                                ProfileImage.Source = new UriImageSource
                                {
                                    Uri = new Uri(user.ProfilePictureUrl),
                                    CachingEnabled = true,
                                    CacheValidity = TimeSpan.FromDays(1)
                                };
                            }
                        });

                        if (user.Friends != null)
                        {
                            _friendList.Clear();
                            foreach (var friendUid in user.Friends)
                            {
                                _friendList.Add(friendUid);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error fetching user data: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"An error occurred while fetching user data: {ex.Message}", "OK");
            }
        }

        private async void OnFriendSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                var friendInviteCode = e.CurrentSelection[0] as string;
                if (!string.IsNullOrWhiteSpace(friendInviteCode))
                {
                    var friendProfile = (await _firebaseClient
                        .Child("users")
                        .OrderBy("inviteCode")
                        .EqualTo(friendInviteCode)
                        .OnceAsync<UserModel>())
                        .FirstOrDefault();

                    if (friendProfile != null)
                    {
                        var friendUid = friendProfile.Key;

                        await Navigation.PushAsync(new ChatPage(_userId, friendUid));
                    }
                    else
                    {
                        await DisplayAlert("Error", "Could not find friend by invite code.", "OK");
                    }
                }

                FriendsCollectionView.SelectedItem = null;
            }
        }


    }


}

