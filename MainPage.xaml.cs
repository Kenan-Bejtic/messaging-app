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
using System.Collections.Generic;
using Microsoft.Maui.Dispatching;

namespace LoginWithFirebase
{
    public partial class MainPage : ContentPage
    {
        private FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;

        
        private HashSet<string> _uniqueChatIds = new HashSet<string>();

        private ObservableCollection<FriendDisplayModel> _chatList = new ObservableCollection<FriendDisplayModel>();

        public ObservableCollection<FriendDisplayModel> FriendList => _chatList;

        private string _userId;
        private string _currentOpenChatUid = null;

        public MainPage(FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            _firebaseAuthClient = firebaseAuthClient;

            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _userId = Preferences.Default.Get("UserId", string.Empty);

            ChatsCollectionView.ItemsSource = _chatList;

            if (string.IsNullOrWhiteSpace(_userId))
            {
                Console.WriteLine("[ERROR] User ID is empty. Cannot load chat data.");
                return;
            }

            _ = LoadChatData();

            SubscribeToChatChanges();

            SubscribeToAllMessages();
        }

        private async Task LoadChatData()
        {
            try
            {
                var user = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .OnceSingleAsync<UserModel>();

                if (user != null)
                {
                    if (user.Friends != null && user.Friends.Any())
                    {
                        _chatList.Clear();
                        _uniqueChatIds.Clear();

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
                                Console.WriteLine($"[WARNING] No user found with inviteCode = {friendInviteCode}");
                                continue;
                            }

                            var realUid = matchingUser.Key;
                            var friendData = matchingUser.Object;

                            if (friendData != null && !_uniqueChatIds.Contains(realUid))
                            {
                                var chatDisplay = new FriendDisplayModel
                                {
                                    FirebaseUid = realUid,
                                    Username = friendData.Username,
                                    ProfilePictureUrl = friendData.ProfilePictureUrl
                                };

                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    Console.WriteLine($"[INFO] Adding chat with: {chatDisplay.Username}");
                                    _chatList.Add(chatDisplay);
                                    _uniqueChatIds.Add(realUid);
                                });
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[ERROR] No user data found at 'users/{_userId}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error fetching chat data: {ex.Message}");
                await DisplayAlert("Error",
                    $"An error occurred while fetching chat data: {ex.Message}", "OK");
            }
        }

        private void SubscribeToChatChanges()
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

                                
                                if (!_uniqueChatIds.Contains(realUid))
                                {
                                    var chatDisplay = new FriendDisplayModel
                                    {
                                        FirebaseUid = realUid,
                                        Username = friendData.Username,
                                        ProfilePictureUrl = friendData.ProfilePictureUrl
                                    };

                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        Console.WriteLine($"[INFO] Adding new chat with: {chatDisplay.Username}");
                                        _chatList.Add(chatDisplay);
                                        _uniqueChatIds.Add(realUid);
                                    });
                                }
                            }
                        }
                    }
                });
        }

        private async void OnChatSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
                {
                    var selectedChat = e.CurrentSelection.FirstOrDefault() as FriendDisplayModel;
                    if (selectedChat != null)
                    {
                        selectedChat.HasUnreadMessages = false;

                        _currentOpenChatUid = selectedChat.FirebaseUid;

                        
                        await Navigation.PushAsync(new ChatPage(_userId, selectedChat.FirebaseUid, selectedChat.ProfilePictureUrl, selectedChat.Username));
                    }
                    ChatsCollectionView.SelectedItem = null;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"[ERROR] ArgumentOutOfRangeException in OnChatSelected: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred while selecting a chat.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in OnChatSelected: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
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
                                if (_currentOpenChatUid != newMsg.FromUserId)
                                {
                                    var chat = _chatList.FirstOrDefault(f => f.FirebaseUid == newMsg.FromUserId);
                                    if (chat != null)
                                    {
                                        chat.HasUnreadMessages = true;
                                    }
                                }
                            });
                        }
                    }
                });
        }

        
        private async void OnProfileButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage(_firebaseAuthClient));
        }
    }
}
