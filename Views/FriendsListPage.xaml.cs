using LoginWithFirebase.Model;
using LoginWithFirebase.ViewModel;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace LoginWithFirebase.Views
{
    public partial class FriendsListPage : ContentPage
    {
        public ObservableCollection<FriendItem> Friends { get; set; }
        public ObservableCollection<GroupItem> Groups { get; set; }

        public ICommand DeleteFriendCommand { get; }
        public ICommand LeaveGroupCommand { get; }

        private string _currentUserId;
        private ChatService _chatService;

        public FriendsListPage(string currentUserId)
        {
            InitializeComponent();
            _currentUserId = currentUserId;
            _chatService = new ChatService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");

            Friends = new ObservableCollection<FriendItem>();
            Groups = new ObservableCollection<GroupItem>();

            DeleteFriendCommand = new Command<string>(async (friendId) => await DeleteFriend(friendId));
            LeaveGroupCommand = new Command<string>(async (groupId) => await LeaveGroup(groupId));

            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFriends();
            await LoadGroups();
        }

        private async Task LoadFriends()
        {
            Friends.Clear();
            try
            {
                var friendIds = await _chatService.GetFriendsAsync(_currentUserId);
                foreach (var friendId in friendIds)
                {
                    var friend = await _chatService.GetUserAsync(friendId);
                    if (friend != null)
                    {
                        Friends.Add(new FriendItem
                        {
                            Id = friendId,
                            Name = friend.Username // Assuming UserModel has Username property
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load friends: {ex.Message}", "OK");
            }
        }

        private async Task LoadGroups()
        {
            Groups.Clear();
            try
            {
                var allGroups = await GetAllGroups();
                foreach (var group in allGroups)
                {
                    Groups.Add(new GroupItem
                    {
                        Id = group.Key,
                        Name = group.Value["groupName"]?.ToString() ?? "Unnamed Group"
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load groups: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Retrieves all groups the current user is a participant of.
        /// </summary>
        /// <returns>A dictionary of groupChatId to group data.</returns>
        private async Task<Dictionary<string, Dictionary<string, object>>> GetAllGroups()
        {
            var userGroups = new Dictionary<string, Dictionary<string, object>>();

            // Fetch all chats
            var chats = await _chatService.GetAllChatsAsync();

            foreach (var chat in chats)
            {
                if (chat.Value.ContainsKey("isGroup") && (bool)chat.Value["isGroup"] == true)
                {
                    if (chat.Value.ContainsKey("participants") && chat.Value["participants"] is Dictionary<string, object> participants)
                    {
                        if (participants.ContainsKey(_currentUserId) && (bool)participants[_currentUserId] == true)
                        {
                            userGroups.Add(chat.Key, chat.Value);
                        }
                    }
                }
            }

            return userGroups;
        }

        private async Task DeleteFriend(string friendId)
        {
            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to remove this friend?", "Yes", "No");
            if (!confirm)
                return;

            try
            {
                await _chatService.RemoveFriendAsync(_currentUserId, friendId);
                // Remove from local list
                var friend = Friends.FirstOrDefault(f => f.Id == friendId);
                if (friend != null)
                    Friends.Remove(friend);

                await DisplayAlert("Success", "Friend removed successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to remove friend: {ex.Message}", "OK");
            }
        }

        private async Task LeaveGroup(string groupId)
        {
            bool confirm = await DisplayAlert("Confirm", "Are you sure you want to leave this group?", "Yes", "No");
            if (!confirm)
                return;

            try
            {
                await _chatService.LeaveGroupAsync(groupId, _currentUserId);
                // Remove from local list
                var group = Groups.FirstOrDefault(g => g.Id == groupId);
                if (group != null)
                    Groups.Remove(group);

                await DisplayAlert("Success", "You have left the group successfully.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to leave group: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Represents a friend item.
    /// </summary>
    public class FriendItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Represents a group item.
    /// </summary>
    public class GroupItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
