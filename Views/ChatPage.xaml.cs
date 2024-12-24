using LoginWithFirebase.Model;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Firebase.Database.Streaming;
using LoginWithFirebase.ViewModel;
using System.Reactive.Linq;

namespace LoginWithFirebase.Views
{
    public partial class ChatPage : ContentPage
    {
        private string _currentUserId;
        private string _friendUserId;
        private ChatService _chatService;
        private ObservableCollection<MessageModel> _messages;
        private string _conversationId;

        public ChatPage(string currentUserId, string friendUserId)
        {
            InitializeComponent();  

            _currentUserId = currentUserId;
            _friendUserId = friendUserId;

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
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
    }
}
