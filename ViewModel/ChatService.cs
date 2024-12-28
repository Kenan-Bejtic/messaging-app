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

        public async Task<bool> GroupExistsAsync(string groupChatId)
        {
            try
            {
                var group = await _firebaseClient
                    .Child("chats")
                    .Child(groupChatId)
                    .OnceSingleAsync<object>();

                return group != null;
            }
            catch (FirebaseException)
            {
                
                return false;
            }
            catch (Exception)
            {
                throw;
            }
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
                profilePictureUrl = "https://firebasestorage.googleapis.com/v0/b/razvoj-mobilnih-aplikacija.appspot.com/o/default_profile_pictures%2Fgroup-chat.png?alt=media&token=90ec02b4-a011-4019-b40d-b3d95ffb7ba6",
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
            try
            {
               
                var participantsRef = _firebaseClient
                    .Child("chats")
                    .Child(groupChatId)
                    .Child("participants");

                
                var userExists = await participantsRef
                    .Child(userId)
                    .OnceSingleAsync<bool?>();

                if (userExists == null)
                {
                   
                    await participantsRef
                        .Child(userId)
                        .PutAsync(true);
                }
                else
                {
                  
                }
            }
            catch (Exception)
            {
                throw;
            }
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


        public async Task<Dictionary<string, Dictionary<string, object>>> GetAllChatsAsync()
        {
            var chats = await _firebaseClient
                .Child("chats")
                .OnceAsync<object>();

            var chatDict = new Dictionary<string, Dictionary<string, object>>();

            foreach (var chat in chats)
            {
                var chatData = chat.Object as Dictionary<string, object>;
                if (chatData != null)
                {
                    chatDict.Add(chat.Key, chatData);
                }
            }

            return chatDict;
        }



        public async Task RemoveFriendAsync(string userId, string friendId)
        {
            try
            {
                
                var userFriends = await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .Child("Friends")
                    .OnceAsync<string>();

                var updatedUserFriends = userFriends.Select(f => f.Object).ToList();
                if (updatedUserFriends.Contains(friendId))
                {
                    updatedUserFriends.Remove(friendId);
                    await _firebaseClient
                        .Child("users")
                        .Child(userId)
                        .Child("Friends")
                        .PutAsync(updatedUserFriends);
                }

                
                var friendFriends = await _firebaseClient
                    .Child("users")
                    .Child(friendId)
                    .Child("Friends")
                    .OnceAsync<string>();

                var updatedFriendFriends = friendFriends.Select(f => f.Object).ToList();
                if (updatedFriendFriends.Contains(userId))
                {
                    updatedFriendFriends.Remove(userId);
                    await _firebaseClient
                        .Child("users")
                        .Child(friendId)
                        .Child("Friends")
                        .PutAsync(updatedFriendFriends);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

       
        public async Task LeaveGroupAsync(string groupChatId, string userId)
        {
            try
            {
                
                var participantsRef = _firebaseClient
                    .Child("chats")
                    .Child(groupChatId)
                    .Child("participants");

                
                var userExists = await participantsRef
                    .Child(userId)
                    .OnceSingleAsync<bool?>();

                if (userExists == true)
                {
                   
                    await participantsRef
                        .Child(userId)
                        .PutAsync(false);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
