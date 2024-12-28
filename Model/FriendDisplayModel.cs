using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LoginWithFirebase.Model
{
    public class FriendDisplayModel : INotifyPropertyChanged
    {
        public string FirebaseUid { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        private bool _hasUnreadMessages;
        public bool HasUnreadMessages
        {
            get => _hasUnreadMessages;
            set
            {
                if (_hasUnreadMessages != value)
                {
                    _hasUnreadMessages = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
