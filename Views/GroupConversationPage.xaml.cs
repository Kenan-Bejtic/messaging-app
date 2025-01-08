using LoginWithFirebase.Model;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Firebase.Database.Streaming;
using System.Reactive.Linq;
using Firebase.Storage;
using LoginWithFirebase.Helpers;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using LoginWithFirebase.ViewModel;
using System.Threading.Tasks;

namespace LoginWithFirebase.Views
{
    public partial class GroupConversationPage : ContentPage
    {
        private string _currentUserId;
        private string _groupChatId;
        private string _groupName;
        private string _groupImageUrl;

        private ChatService _chatService;
        private ObservableCollection<MessageModel> _messages;
        private Dictionary<string, string> _usernamesCache;

        public string GroupName { get; set; }
        public string GroupImageUrl { get; set; }

        public GroupConversationPage(
            string currentUserId,
            string groupChatId,
            string groupImageUrl = "",
            string groupName = "Group Chat")
        {
            InitializeComponent();

            BubbleAlignmentConverter.CurrentUserId = currentUserId;
            BubbleBackgroundColorConverter.CurrentUserId = currentUserId;

            _currentUserId = currentUserId;
            _groupChatId = groupChatId;
            _groupName = groupName;
            _groupImageUrl = string.IsNullOrEmpty(groupImageUrl)
                                ? "placeholder_group.png"
                                : groupImageUrl;

            GroupName = _groupName;
            GroupImageUrl = _groupImageUrl;
            BindingContext = this;

            _chatService = new ChatService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _messages = new ObservableCollection<MessageModel>();
            MessagesCollectionView.ItemsSource = _messages;

            _usernamesCache = new Dictionary<string, string>();

            LoadGroupConversation();
        }

        private async void LoadGroupConversation()
        {
            try
            {
                
                var actualGroupName = await _chatService.GetGroupNameAsync(_groupChatId);
                GroupName = $"{actualGroupName} ({_groupChatId})";
                OnPropertyChanged(nameof(GroupName));

                
                var existingMessages = await _chatService.GetGroupMessagesAsync(_groupChatId);
                foreach (var msg in existingMessages)
                {
                    await PopulateSenderUsername(msg);
                    ConfigureMessageProperties(msg);
                    _messages.Add(msg);
                }

                
                if (_messages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1],
                            position: ScrollToPosition.End,
                            animate: false);
                    });
                }

                
                var observable = _chatService.SubscribeToGroupMessages(_groupChatId);
                observable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(fbEvent =>
                    {
                        
                        HandleGroupMessageEventAsync(fbEvent).ConfigureAwait(false);
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async Task HandleGroupMessageEventAsync(FirebaseEvent<MessageModel> fbEvent)
        {
            try
            {
                if (fbEvent.EventType == FirebaseEventType.InsertOrUpdate)
                {
                    var newMsg = fbEvent.Object;
                    if (newMsg == null) return;

                    bool messageExists = _messages.Any(m =>
                        m.Timestamp == newMsg.Timestamp &&
                        m.Content == newMsg.Content &&
                        m.FromUserId == newMsg.FromUserId);

                    if (!messageExists)
                    {
                        await PopulateSenderUsername(newMsg);
                        ConfigureMessageProperties(newMsg);

                        
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _messages.Add(newMsg);
                        });

                        
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            if (_messages.Count > 0)
                            {
                                MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1],
                                    position: ScrollToPosition.End,
                                    animate: true);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error handling group message event: {ex.Message}");
            }
        }

        private void ConfigureMessageProperties(MessageModel msg)
        {
            bool isFromMe = (msg.FromUserId == _currentUserId);
            bool hasImage = !string.IsNullOrEmpty(msg.ImageUrl);
            bool hasText = !string.IsNullOrEmpty(msg.Content);

            msg.ShowUsername = (!isFromMe) && hasText;
            msg.UsernameLine = msg.ShowUsername ? msg.SenderUsername : "";
        }

        private async Task PopulateSenderUsername(MessageModel msg)
        {
            if (string.IsNullOrEmpty(msg.FromUserId)) return;

            if (!_usernamesCache.ContainsKey(msg.FromUserId))
            {
                var userData = await _chatService.GetUserByIdAsync(msg.FromUserId);
                if (userData != null && !string.IsNullOrEmpty(userData.Username))
                {
                    _usernamesCache[msg.FromUserId] = userData.Username;
                }
                else
                {
                    _usernamesCache[msg.FromUserId] = "Unknown";
                }
            }

            msg.SenderUsername = _usernamesCache[msg.FromUserId];
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var newMessage = new MessageModel
                {
                    FromUserId = _currentUserId,
                    Content = text,
                    Timestamp = DateTime.UtcNow,
                    ToUserId = "",
                    ChatId = _groupChatId
                };

                try
                {
                    await _chatService.SendGroupMessageAsync(_groupChatId, newMessage);
                    MessageEntry.Text = string.Empty;

                    if (_messages.Count > 0)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1],
                                position: ScrollToPosition.End,
                                animate: true);
                        });
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        private async void OnSendImageClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync();
                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                var storage = new FirebaseStorage("razvoj-mobilnih-aplikacija.appspot.com");
                var fileName = $"{_currentUserId}_{Path.GetFileName(result.FullPath)}";

                
                await storage
                    .Child("chat_images")
                    .Child(fileName)
                    .PutAsync(stream);

                
                var downloadUrl = await storage
                    .Child("chat_images")
                    .Child(fileName)
                    .GetDownloadUrlAsync();

                var newMessage = new MessageModel
                {
                    FromUserId = _currentUserId,
                    Content = "",
                    ImageUrl = downloadUrl,
                    Timestamp = DateTime.UtcNow,
                    ToUserId = "",
                    ChatId = _groupChatId
                };

                await _chatService.SendGroupMessageAsync(_groupChatId, newMessage);

                if (_messages.Count > 0)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1],
                            position: ScrollToPosition.End,
                            animate: true);
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
