using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Model
{
    public class FriendRequest
    {
        public string FromUserId { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } 
        public string ProfilePictureUrl { get; set; }
    }

}
