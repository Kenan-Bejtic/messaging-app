using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Model
{
    public class UserModel
    {
        public string Username { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public List<string> Friends { get; set; } = new List<string>();
    }
}
