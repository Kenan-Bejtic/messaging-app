using LoginWithFirebase.Model;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Firebase.Database.Streaming;
using LoginWithFirebase.ViewModel;
using System.Reactive.Linq;
using Firebase.Storage;
using LoginWithFirebase.Helpers;
using System.Threading;

namespace LoginWithFirebase.Views
{
    public partial class ChatPage : ContentPage
    {
        private string _currentUserId;
        private string _friendUserId;
        private ChatService _chatService;
        private ObservableCollection<MessageModel> _messages;
        private string _conversationId;

        public string FriendProfilePictureUrl { get; set; }
        public string FriendUsername { get; set; }

        public ChatPage(string currentUserId, string friendUserId, string friendProfilePictureUrl, string friendUsername)
        {
            InitializeComponent();
            BubbleAlignmentConverter.CurrentUserId = currentUserId;
            BubbleBackgroundColorConverter.CurrentUserId = currentUserId;

            _currentUserId = currentUserId;
            _friendUserId = friendUserId;
            FriendProfilePictureUrl = friendProfilePictureUrl;
            FriendUsername = friendUsername;

            BindingContext = this;

            _chatService = new ChatService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            _messages = new ObservableCollection<MessageModel>();
            MessagesCollectionView.ItemsSource = _messages;

            _conversationId = _chatService.GenerateConversationId(_currentUserId, _friendUserId);

            LoadConversation();
        }

        private async void LoadConversation()
        {
            try
            {
                var existingMessages = await _chatService.GetAllMessagesAsync(_conversationId);

                foreach (var msg in existingMessages)
                {
                    _messages.Add(msg);
                }

                if (_messages.Count > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1], position: ScrollToPosition.End, animate: false);
                    });
                }

                var observable = _chatService.SubscribeToMessages(_conversationId);

                observable
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(fbEvent =>
                    {
                        if (fbEvent.EventType == FirebaseEventType.InsertOrUpdate)
                        {
                            var newMsg = fbEvent.Object;
                            if (newMsg == null) return;

                            if (!_messages.Any(m =>
                                m.Timestamp == newMsg.Timestamp &&
                                m.Content == newMsg.Content &&
                                m.FromUserId == newMsg.FromUserId))
                            {
                                _messages.Add(newMsg);

                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    if (_messages.Count > 0)
                                    {
                                        MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1], position: ScrollToPosition.End, animate: true);
                                    }
                                });
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            var text = MessageEntry.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var newMessage = new MessageModel
                {
                    FromUserId = _currentUserId,
                    ToUserId = _friendUserId,
                    Content = text,
                    Timestamp = DateTime.UtcNow
                };

                try
                {
                    await _chatService.SendMessageAsync(_conversationId, newMessage);
                    MessageEntry.Text = string.Empty;

                    if (_messages.Count > 0)
                    {
                        MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1], position: ScrollToPosition.End, animate: true);
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

                var uploadTask = await storage
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
                    ToUserId = _friendUserId,
                    Content = "",
                    ImageUrl = downloadUrl,
                    Timestamp = DateTime.UtcNow
                };

                await _chatService.SendMessageAsync(_conversationId, newMessage);

                if (_messages.Count > 0)
                {
                    MessagesCollectionView.ScrollTo(_messages[_messages.Count - 1], position: ScrollToPosition.End, animate: true);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
