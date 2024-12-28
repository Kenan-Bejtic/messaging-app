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
using Firebase.Database.Streaming;
using System.Diagnostics;

namespace LoginWithFirebase
{
    public partial class MainPage : ContentPage
    {
        private FirebaseAuthClient _firebaseAuthClient;

        public Command<FriendDisplayModel> ItemTappedCommand { get; }

        private readonly FirebaseClient _firebaseClient;

        private HashSet<string> _uniqueChatIds = new HashSet<string>();

        private ObservableCollection<FriendDisplayModel> _chatList = new ObservableCollection<FriendDisplayModel>();

        public ObservableCollection<FriendDisplayModel> FriendList => _chatList;

      



        private string _userId;
        private string _currentOpenChatUid = null;


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

        
        private IDisposable _friendRequestsSubscription;

        public MainPage(FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            BindingContext = this;
            _firebaseAuthClient = firebaseAuthClient;

            ItemTappedCommand = new Command<FriendDisplayModel>(async (selectedChat) => await OnChatTapped(selectedChat));

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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadUserData();
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
                    
                    await LoadChatData();

                    
                    await CheckPendingFriendRequests(user.InviteCode);

                    SubscribeToFriendRequestChanges(user.InviteCode);
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

        private async Task LoadChatData()
        {
            try
            {
               
                var user = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .OnceSingleAsync<UserModel>();

                if (user == null)
                {
                    Console.WriteLine($"[ERROR] No user data found at 'users/{_userId}'.");
                    return;
                }

                _chatList.Clear();
                _uniqueChatIds.Clear();

               
                if (user.Friends != null && user.Friends.Any())
                {
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
                                ProfilePictureUrl = friendData.ProfilePictureUrl,
                                IsGroup = false
                            };

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Console.WriteLine($"[INFO] Adding single chat: {chatDisplay.Username}");
                                _chatList.Add(chatDisplay);
                                _uniqueChatIds.Add(realUid);
                            });
                        }
                    }
                }

               
                var allChats = await _firebaseClient
                    .Child("chats")
                    .OnceAsync<dynamic>();
                

                foreach (var chat in allChats)
                {
                    

                    try
                    {
                        var isGroupObj = chat.Object.isGroup;      
                        if (isGroupObj == null) continue;
                        bool isGroup = (bool)isGroupObj;
                        if (!isGroup) continue;                    

                        
                        var participants = chat.Object.participants;
                        if (participants == null) continue;

                        
                        bool userIsParticipant = false;
                      
                        try
                        {
                            userIsParticipant = participants[_userId] == true;
                        }
                        catch {  }

                        if (!userIsParticipant) continue;        

                        
                        string groupName = (string)chat.Object.groupName;

                        
                        if (!_uniqueChatIds.Contains(chat.Key))
                        {
                            var groupChatDisplay = new FriendDisplayModel
                            {
                                FirebaseUid = chat.Key,        
                                Username = groupName,         
                                ProfilePictureUrl = "https://firebasestorage.googleapis.com/v0/b/razvoj-mobilnih-aplikacija.appspot.com/o/default_profile_pictures%2Fgroup-chat.png?alt=media&token=d14cdcf9-995a-449f-96ce-86701c5d8de1", 
                                IsGroup = true
                            };

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Console.WriteLine($"[INFO] Adding group chat: {groupChatDisplay.Username}");
                                _chatList.Add(groupChatDisplay);
                                _uniqueChatIds.Add(chat.Key);
                            });
                        }
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[WARNING] Could not process chat {chat.Key}: {ex2.Message}");
                    }
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


        private async Task OnChatTapped(FriendDisplayModel selectedChat)
        {
            if (selectedChat == null) return;

            
            selectedChat.HasUnreadMessages = false;

            
            _currentOpenChatUid = selectedChat.FirebaseUid;

            
            try
            {
                if (selectedChat.IsGroup)
                {
                    
                    await Navigation.PushAsync(new GroupConversationPage(
                        currentUserId: _userId,
                        groupChatId: selectedChat.FirebaseUid
                    ));
                }
                else
                {
                   
                    await Navigation.PushAsync(new ChatPage(
                        _userId,
                        selectedChat.FirebaseUid,
                        selectedChat.ProfilePictureUrl,
                        selectedChat.Username
                    ));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in OnChatTapped: {ex.Message}");
                await DisplayAlert("Error", "An unexpected error occurred.", "OK");
            }
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
                        Console.WriteLine($"[DEBUG] Selected Chat: {selectedChat.Username}");
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

        private List<IDisposable> _messageSubscriptions = new List<IDisposable>();

        private void SubscribeToAllMessages()
        {
            foreach (var chatItem in _chatList)
            {
                var chatId = chatItem.FirebaseUid;

                Console.WriteLine($"[DEBUG] Setting up subscription for chat '{chatId}'");

                var sub = _firebaseClient
                    .Child("chats")
                    .Child(chatId)
                    .Child("messages")
                    .AsObservable<MessageModel>()
                    .Subscribe(fbEvent =>
                    {
                        try
                        {
                            Console.WriteLine($"[DEBUG] Subscription triggered for chat {chatId}: {fbEvent.EventType}");

                            if (fbEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                            {
                                var newMsg = fbEvent.Object;
                                if (newMsg == null)
                                {
                                    Console.WriteLine("[DEBUG] newMsg is null, skipping...");
                                    return;
                                }

                                Console.WriteLine($"[DEBUG] FromUserId='{newMsg.FromUserId}', " +
                                                  $"ToUserId='{newMsg.ToUserId}', " +
                                                  $"ChatId='{newMsg.ChatId}'");

                                if (string.IsNullOrEmpty(newMsg.ToUserId)
                                    && newMsg.FromUserId != _userId
                                    && !string.IsNullOrEmpty(newMsg.ChatId)
                                    && _currentOpenChatUid != newMsg.ChatId)
                                {
                                    Console.WriteLine("[DEBUG] Group chat logic triggered");
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        var groupChat = _chatList.FirstOrDefault(
                                            f => f.FirebaseUid == newMsg.ChatId && f.IsGroup);
                                        if (groupChat != null)
                                            groupChat.HasUnreadMessages = true;
                                    });
                                }
                               
                                else if (!string.IsNullOrEmpty(newMsg.ToUserId)
                                         && newMsg.ToUserId == _userId
                                         && newMsg.FromUserId != _userId
                                         && _currentOpenChatUid != newMsg.FromUserId)
                                {
                                    Console.WriteLine("[DEBUG] Single chat logic triggered");
                                    MainThread.BeginInvokeOnMainThread(() =>
                                    {
                                        var singleChat = _chatList.FirstOrDefault(
                                            f => f.FirebaseUid == newMsg.FromUserId && !f.IsGroup);
                                        if (singleChat != null)
                                            singleChat.HasUnreadMessages = true;
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] in subscription for chat {chatId}: {ex}");
                        }
                    });

                _messageSubscriptions.Add(sub);
            }
        }


        private async void OnProfileButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage(_firebaseAuthClient));
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

        private void SubscribeToFriendRequestChanges(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                HasPendingFriendRequests = false;
                return;
            }

            _friendRequestsSubscription = _firebaseClient
                .Child("friend_requests")
                .OrderBy("toInviteCode")
                .EqualTo(inviteCode)
                .AsObservable<FriendRequest>()
                .Subscribe(async fbEvent =>
                {
                    if (fbEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate ||
                        fbEvent.EventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                    {
                        await CheckPendingFriendRequests(inviteCode);
                    }
                });
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _friendRequestsSubscription?.Dispose();
        }

        private async void OnGroupChatButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GroupChatPage(_userId));
        }
    }

}
