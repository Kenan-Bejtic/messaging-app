using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Model
{
    public class FriendDisplayModel
    {
        public string FirebaseUid { get; set; } = string.Empty;       
        public string Username { get; set; } = string.Empty;         
        public string ProfilePictureUrl { get; set; } = string.Empty;


        private bool _hasUnreadMessages;
        public bool HasUnreadMessages
        {
            get => _hasUnreadMessages;
            set
            {
                if (_hasUnreadMessages != value)
                {
                    _hasUnreadMessages = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnreadMessages)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
