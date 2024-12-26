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
        private FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;

        private ObservableCollection<FriendDisplayModel> _friendList = new ObservableCollection<FriendDisplayModel>();

        private string _userId;
        private string _currentOpenChatUid = null;
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

            
            _ = LoadUserData();

            
            SubscribeToFriendChanges();

            SubscribeToAllMessages();
        }

        private void SubscribeToFriendChanges()
        {
            
            var observable = _firebaseClient
                .Child("users")
                .Child(_userId)
                .Child("friends")
                .AsObservable<string>();  

            observable
                .Subscribe(async fbEvent =>
                {
                    if (fbEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        var friendInviteCode = fbEvent.Object; 
                        if (!string.IsNullOrEmpty(friendInviteCode))
                        {
                          
                            var matchingUser = (await _firebaseClient
                                .Child("users")
                                .OrderBy("inviteCode")
                                .EqualTo(friendInviteCode)
                                .OnceAsync<UserModel>())
                                .FirstOrDefault();

                            if (matchingUser != null)
                            {
                                var realUid = matchingUser.Key;
                                var friendData = matchingUser.Object;

                              
                                if (!_friendList.Any(f => f.FirebaseUid == realUid))
                                {
                                    var friendDisplay = new FriendDisplayModel
                                    {
                                        FirebaseUid = realUid,
                                        Username = friendData.Username,
                                        ProfilePictureUrl = friendData.ProfilePictureUrl
                                    };
                                    _friendList.Add(friendDisplay);
                                }
                            }
                        }

                    }
                });
        }

        private int count = 0;
        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            CounterBtn.Text = count == 1
                ? $"Clicked {count} time"
                : $"Clicked {count} times";

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

                       
                        if (user.Friends != null && user.Friends.Any())
                        {
                            _friendList.Clear();

                            foreach (var friendInviteCode in user.Friends) 
                            {
                                var matchingUser = (await _firebaseClient
                                    .Child("users")
                                    .OrderBy("inviteCode")
                                    .EqualTo(friendInviteCode) 
                                    .OnceAsync<UserModel>())
                                    .FirstOrDefault();

                                if (matchingUser == null)
                                {
                                    Console.WriteLine($"No user found with inviteCode = {friendInviteCode}");
                                    continue;
                                }

                                var realUid = matchingUser.Key;
                                var friendData = matchingUser.Object;
                               

                                if (friendData != null)
                                {
                                    var friendDisplay = new FriendDisplayModel
                                    {
                                        FirebaseUid = realUid,              
                                        Username = friendData.Username,
                                        ProfilePictureUrl = friendData.ProfilePictureUrl
                                    };
                                    _friendList.Add(friendDisplay);
                                }
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine($"[ERROR] No user data found at 'users/{_userId}'.");
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
                var selectedFriend = e.CurrentSelection[0] as FriendDisplayModel;
                if (selectedFriend != null)
                {
                    selectedFriend.HasUnreadMessages = false;

                    _currentOpenChatUid = selectedFriend.FirebaseUid;

                    // Pass friend's profile picture URL and username
                    await Navigation.PushAsync(new ChatPage(_userId, selectedFriend.FirebaseUid, selectedFriend.ProfilePictureUrl, selectedFriend.Username));
                }
                FriendsCollectionView.SelectedItem = null;
            }
        }



        private void SubscribeToAllMessages()
        {
           

            var observable = _firebaseClient
                .Child("chats")
                .AsObservable<MessageModel>()
                .Subscribe(fbEvent =>
                {
                    if (fbEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                    {
                        var newMsg = fbEvent.Object;
                        if (newMsg == null) return;

                       
                        if (newMsg.ToUserId == _userId)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                if (newMsg.ToUserId == _userId)
{
    if (_currentOpenChatUid != newMsg.FromUserId)
    {
        var friend = _friendList.FirstOrDefault(f => f.FirebaseUid == newMsg.FromUserId);
        if (friend != null)
        {
            friend.HasUnreadMessages = true;
        }
    }
}
                            });
                        }
                    }
                });
        }

    }


}
