using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.ViewModel
{
    public partial class FriendInvitationViewModel : ObservableObject
    {
        private readonly FirebaseClient _firebaseClient;

        [ObservableProperty]
        private ObservableCollection<FriendRequest> pendingRequests;

        [ObservableProperty]
        private string enteredInviteCode;

        [ObservableProperty]
        private string searchedInviteCode;

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string profilePictureUrl;

        [ObservableProperty]
        private bool isProfileVisible;

        [ObservableProperty]
        private string currentUserInviteCode;


        public FriendInvitationViewModel(FirebaseClient firebaseClient, string currentUserInviteCode)
        {
            _firebaseClient = firebaseClient;
            CurrentUserInviteCode = currentUserInviteCode;

            
            PendingRequests = new ObservableCollection<FriendRequest>();

            
            if (string.IsNullOrEmpty(CurrentUserInviteCode))
            {
                Console.WriteLine("Error: CurrentUserInviteCode is null or empty.");
            }
        }



        [RelayCommand]
        public async Task SearchAsync(string inviteCode)
        {
            
            if (string.IsNullOrWhiteSpace(inviteCode) || !inviteCode.StartsWith("#") || inviteCode.Length != 7)
            {
                await Application.Current.MainPage.DisplayAlert("Invalid Code", "The invite code must be in the format: #123456.", "OK");
                IsProfileVisible = false;
                return;
            }

            
            string codeToSearch = inviteCode.Substring(1);

            
            if (codeToSearch == CurrentUserInviteCode)
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You cannot send a friend request to yourself.", "OK");
                return;
            }

            try
            {
                
                var currentUserFriends = await _firebaseClient
                    .Child("users")
                    .Child(CurrentUserInviteCode) 
                    .Child("friends")
                    .OnceAsync<string>();

                
                var friendsList = currentUserFriends.Select(f => f.Object).ToList();
                if (friendsList.Contains(codeToSearch))
                {
                    await Application.Current.MainPage.DisplayAlert("Info", "You are already friends with this user.", "OK");
                    return;
                }

                Console.WriteLine($"Searching for code: {codeToSearch}");

                
                var user = (await _firebaseClient
                    .Child("users")
                    .OrderBy("inviteCode")
                    .EqualTo(codeToSearch)
                    .OnceAsync<dynamic>())
                    .FirstOrDefault();

                if (user != null)
                {
                    
                    Username = user.Object.username;
                    ProfilePictureUrl = user.Object.profilePictureUrl;
                    SearchedInviteCode = inviteCode;
                    IsProfileVisible = true;
                }
                else
                {
                    
                    await Application.Current.MainPage.DisplayAlert("User Not Found", "No user found with this invite code.", "OK");
                    IsProfileVisible = false;
                }
            }
            catch (Firebase.Database.FirebaseException ex)
            {
               
                Console.WriteLine($"Firebase error: {ex.Message}");
                Console.WriteLine($"Firebase error details: {ex.InnerException?.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", "An error occurred while searching. Please try again.", "OK");
                IsProfileVisible = false;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"General error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
                IsProfileVisible = false;
            }
        }



        [RelayCommand]
        public async Task SendFriendRequestAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchedInviteCode))
            {
                return;
            }

            if (SearchedInviteCode == $"#{CurrentUserInviteCode}")
            {
                await Application.Current.MainPage.DisplayAlert("Error", "You cannot send a friend request to yourself.", "OK");
                return;
            }

            try
            {
                string currentUserId = CurrentUserInviteCode.TrimStart('#');
                string recipientInviteCode = SearchedInviteCode.TrimStart('#');

                
                var currentUserProfile = await GetUserProfileAndFriends(currentUserId);
                if (currentUserProfile.Friends.Contains(recipientInviteCode))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "You are already friends with this user.", "OK");
                    return;
                }

                
                var pendingRequest = (await _firebaseClient
                    .Child("friend_requests")
                    .OnceAsync<dynamic>())
                    .FirstOrDefault(r =>
                        (r.Object.fromUserId == currentUserId && r.Object.toInviteCode == recipientInviteCode && r.Object.status == "pending") ||
                        (r.Object.fromUserId == recipientInviteCode && r.Object.toInviteCode == currentUserId && r.Object.status == "pending"));

                if (pendingRequest != null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "A friend request is already pending with this user.", "OK");
                    return;
                }

               
                await _firebaseClient
                    .Child("friend_requests")
                    .PostAsync(new
                    {
                        fromUserId = currentUserId,
                        toInviteCode = recipientInviteCode,
                        status = "pending",
                        timestamp = DateTime.UtcNow
                    });

                await Application.Current.MainPage.DisplayAlert("Success", "Friend request sent successfully.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }


        [RelayCommand]
        public async Task AcceptFriendRequestAsync(string senderInviteCode)
        {
            try
            {
               
                var request = (await _firebaseClient
                    .Child("friend_requests")
                    .OrderBy("fromUserId")
                    .EqualTo(senderInviteCode)
                    .OnceAsync<dynamic>())
                    .FirstOrDefault(r => r.Object.toInviteCode == CurrentUserInviteCode);

                if (request != null)
                {
                    
                    await _firebaseClient
                        .Child("friend_requests")
                        .Child(request.Key)
                        .PutAsync(new { status = "accepted" });

                    
                    var (currentUserKey, currentUserFriends) = await GetUserProfileAndFriends(CurrentUserInviteCode);
                    var (senderUserKey, senderUserFriends) = await GetUserProfileAndFriends(senderInviteCode);

                    if (!string.IsNullOrEmpty(currentUserKey) && !string.IsNullOrEmpty(senderUserKey))
                    {
                        
                        if (!currentUserFriends.Contains(senderInviteCode))
                        {
                            currentUserFriends.Add(senderInviteCode);
                        }

                        
                        await _firebaseClient
                            .Child("users")
                            .Child(currentUserKey)
                            .Child("friends")
                            .PutAsync(currentUserFriends); 

                        if (!senderUserFriends.Contains(CurrentUserInviteCode))
                        {
                            senderUserFriends.Add(CurrentUserInviteCode);
                        }

                        await _firebaseClient
                            .Child("users")
                            .Child(senderUserKey)
                            .Child("friends")
                            .PutAsync(senderUserFriends); 

                        await Application.Current.MainPage.DisplayAlert("Success", "Friend request accepted and friends list updated.", "OK");
                        await LoadPendingRequestsAsync();
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Friend request not found for this user.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

       
        private async Task<(string Key, List<string> Friends)> GetUserProfileAndFriends(string inviteCode)
        {
            try
            {
                
                var userProfile = (await _firebaseClient
                    .Child("users")
                    .OrderBy("inviteCode")
                    .EqualTo(inviteCode)
                    .OnceAsync<dynamic>())
                    .FirstOrDefault();

                if (userProfile != null)
                {
                    
                    List<string> friendsList = new List<string>();
                    try
                    {
                        friendsList = userProfile.Object.friends?.ToObject<List<string>>() ?? new List<string>();
                    }
                    catch (Exception)
                    {
                        
                        var friendsObject = (Newtonsoft.Json.Linq.JObject)userProfile.Object.friends;
                        friendsList = friendsObject.Properties().Select(p => p.Value.ToString()).ToList();
                    }

                    return (userProfile.Key, friendsList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user profile and friends: {ex.Message}");
            }

            return (null, new List<string>());
        }










        [RelayCommand]
        public async Task DeleteFriendRequestAsync(string senderInviteCode)
        {
            try
            {
                
                string currentUserInviteCode = CurrentUserInviteCode;

                
                var request = (await _firebaseClient
                    .Child("friend_requests")
                    .OrderBy("fromUserId")
                    .EqualTo(senderInviteCode)
                    .OnceAsync<dynamic>())
                    .FirstOrDefault(r => r.Object.toInviteCode == currentUserInviteCode);

                if (request != null)
                {
                    
                    await _firebaseClient
                        .Child("friend_requests")
                        .Child(request.Key)
                        .DeleteAsync();

                    await Application.Current.MainPage.DisplayAlert("Success", "Friend request deleted.", "OK");

                   
                    await LoadPendingRequestsAsync();
                }
                else
                {
                    
                    await Application.Current.MainPage.DisplayAlert("Error", "Friend request not found for this user.", "OK");
                }
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error deleting friend request: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }



        [RelayCommand]
        public async Task LoadPendingRequestsAsync()
        {
            try
            {
                
                string currentUserCode = CurrentUserInviteCode.TrimStart('#');

                
                var requests = await _firebaseClient
                    .Child("friend_requests")
                    .OrderBy("toInviteCode")
                    .EqualTo(currentUserCode)
                    .OnceAsync<dynamic>();

                
                PendingRequests.Clear();

                foreach (var request in requests)
                {
                    string senderId = request.Object.fromUserId;

                    
                    var allUsers = await _firebaseClient
                        .Child("users")
                        .OnceAsync<dynamic>();

                    var senderProfile = allUsers
                        .FirstOrDefault(user => user.Object.inviteCode == senderId);

                    if (senderProfile != null)
                    {
                        string senderUsername = senderProfile.Object.username;
                        string profilePicUrl = senderProfile.Object.profilePictureUrl ?? "default_profile_picture.png";

                        Console.WriteLine($"Loaded sender details: {senderUsername}, {profilePicUrl}");

                        
                        PendingRequests.Add(new FriendRequest
                        {
                            FromUserId = senderId,
                            Username = senderUsername,
                            ProfilePictureUrl = profilePicUrl,
                            Status = request.Object.status,
                            Timestamp = request.Object.timestamp
                        });
                    }
                    else
                    {
                        Console.WriteLine($"No user found with invite code: {senderId}");
                        
                        PendingRequests.Add(new FriendRequest
                        {
                            FromUserId = senderId,
                            Username = "Unknown User",
                            ProfilePictureUrl = "default_profile_picture.png",
                            Status = request.Object.status,
                            Timestamp = request.Object.timestamp
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading requests: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred while loading requests: {ex.Message}", "OK");
            }
        }







    }
}

