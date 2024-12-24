using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.Model
{
    public class MessageModel
    {
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
