using Firebase.Database.Streaming;
using Firebase.Database;
using LoginWithFirebase.Model;
using System;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace LoginWithFirebase.ViewModel
{ public class ChatService { 
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
    }
}
