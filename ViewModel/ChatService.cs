using Firebase.Database.Streaming;
using Firebase.Database;
using LoginWithFirebase.Model;
using Firebase.Database.Query;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;


namespace LoginWithFirebase.ViewModel
{
    public class ChatService { 
    private readonly FirebaseClient _firebaseClient;

    public ChatService(string firebaseUrl)
    {
        
        _firebaseClient = new FirebaseClient(firebaseUrl);
    }

    
    public async Task<UserModel> GetUserAsync(string userId)
    {
        return await _firebaseClient
            .Child("users")
            .Child(userId)
            .OnceSingleAsync<UserModel>();
    }

    
    public async Task<List<string>> GetFriendsAsync(string userId)
    {
        var userData = await GetUserAsync(userId);
        if (userData?.Friends != null)
            return userData.Friends;
        return new List<string>();
    }

    
    public string GenerateConversationId(string uidA, string uidB)
    {
        var sorted = new List<string> { uidA, uidB };
        sorted.Sort();
        return $"{sorted[0]}_{sorted[1]}";
    }

    
    public async Task<List<MessageModel>> GetAllMessagesAsync(string conversationId)
    {
        var messages = await _firebaseClient
            .Child("chats")
            .Child(conversationId)
            .Child("messages")
            .OrderByKey() 
            .OnceAsync<MessageModel>();

        return messages.Select(x => x.Object).ToList();
    }

       


        public async Task SendMessageAsync(string conversationId, MessageModel message)
    {
        await _firebaseClient
            .Child("chats")
            .Child(conversationId)
            .Child("messages")
            .PostAsync(message);
    }

   
    public IObservable<FirebaseEvent<MessageModel>> SubscribeToMessages(string conversationId)
    {
        return _firebaseClient
            .Child("chats")
            .Child(conversationId)
            .Child("messages")
            .AsObservable<MessageModel>()
            .SubscribeOn(ThreadPoolScheduler.Instance);
    }








        public async Task<string> CreateGroupAsync(string groupName, string creatorUserId)
        {
            string groupId = Guid.NewGuid().ToString("N").Substring(0, 8);

            var groupData = new
            {
                isGroup = true,
                groupName = groupName,
                participants = new Dictionary<string, bool>
        {
            { creatorUserId, true }
        }
            };

            await _firebaseClient
                .Child("chats")
                .Child(groupId)
                .PutAsync(groupData);

            return groupId;
        }



        public async Task JoinGroupAsync(string groupChatId, string userId)
        {
            
            await _firebaseClient
                .Child("chats")
                .Child(groupChatId)
                .Child("participants")
                .Child(userId)
                .PutAsync(true);
        }


        public async Task<string> GetGroupNameAsync(string groupChatId)
        {
            var chatData = await _firebaseClient
                .Child("chats")
                .Child(groupChatId)
                .OnceSingleAsync<Dictionary<string, object>>();

            if (chatData != null && chatData.ContainsKey("groupName"))
                return chatData["groupName"]?.ToString();

            return null;
        }


        public IObservable<FirebaseEvent<MessageModel>> SubscribeToGroupMessages(string groupChatId)
        {
            return _firebaseClient
                .Child("chats")
                .Child(groupChatId)
                .Child("messages")
                .AsObservable<MessageModel>();
        }

        
        public async Task SendGroupMessageAsync(string groupChatId, MessageModel message)
        {
            await _firebaseClient
                .Child("chats")
                .Child(groupChatId)
                .Child("messages")
                .PostAsync(message);
        }

        
        public async Task<List<MessageModel>> GetGroupMessagesAsync(string groupChatId)
        {
            var msgs = await _firebaseClient
                .Child("chats")
                .Child(groupChatId)
                .Child("messages")
                .OnceAsync<MessageModel>();

            var result = new List<MessageModel>();
            foreach (var m in msgs)
            {
                var model = m.Object;
                
                result.Add(model);
            }

            return result.OrderBy(m => m.Timestamp).ToList();
        }

        
        public async Task<UserModel> GetUserByIdAsync(string userId)
        {
            return await _firebaseClient
                .Child("users")
                .Child(userId)
                .OnceSingleAsync<UserModel>();
        }

    }
}
