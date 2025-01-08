using LoginWithFirebase.ViewModel;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using LoginWithFirebase.Model; 

namespace LoginWithFirebase.Views
{
    public partial class GroupChatPage : ContentPage
    {
        private string _currentUserId;
        private ChatService _chatService;

        public GroupChatPage(string currentUserId)
        {
            InitializeComponent();
            _currentUserId = currentUserId;

            _chatService = new ChatService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
        }

        private async void OnCreateGroupClicked(object sender, EventArgs e)
        {
            var groupName = GroupNameEntry.Text?.Trim();
            if (string.IsNullOrEmpty(groupName))
            {
                await DisplayAlert("Error", "Please enter a group name.", "OK");
                return;
            }

            try
            {
                
                var newGroupChatId = await _chatService.CreateGroupAsync(groupName, _currentUserId);

                await DisplayAlert("Info", $"Grupa '{groupName}' je kreirana!\nID: {newGroupChatId}", "OK");

                await Navigation.PushAsync(new GroupConversationPage(_currentUserId, newGroupChatId, "placeholder_group.png", groupName));

               
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to create group: {ex.Message}", "OK");
            }
            finally
            {
                
            }
        }

        private async void OnJoinGroupClicked(object sender, EventArgs e)
        {
            var groupChatId = GroupIdEntry.Text?.Trim();
            if (string.IsNullOrEmpty(groupChatId))
            {
                await DisplayAlert("Greška", "Unesite ID grupe.", "OK");
                return;
            }

            try
            {
                
                bool groupExists = await _chatService.GroupExistsAsync(groupChatId);

                if (!groupExists)
                {
                   
                    await DisplayAlert("Greška", $"Groupa sa ID '{groupChatId}' ne postoji.", "OK");
                    return;
                }

                
                await _chatService.JoinGroupAsync(groupChatId, _currentUserId);

                await DisplayAlert("Info", $"Uspješno ste se pridružili grupi sa ID: {groupChatId}", "OK");

                
                await Navigation.PushAsync(new GroupConversationPage(_currentUserId, groupChatId, "placeholder_group.png", "Group Chat"));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Greška", $"Greška : {ex.Message}", "OK");
            }
            finally
            {
                
            }
        }
    }
}

