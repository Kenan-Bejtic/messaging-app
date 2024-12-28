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
        public string ImageUrl { get; set; } = string.Empty;

        public string DisplayMessage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Content))
                    return string.Empty;

                return $"{SenderUsername}: {Content}";
            }
        }


        public string SenderUsername { get; set; } 
       
        [Newtonsoft.Json.JsonIgnore]
        public bool ShowUsername { get; set; } 

        [Newtonsoft.Json.JsonIgnore]
        public string UsernameLine { get; set; }
    }
}
