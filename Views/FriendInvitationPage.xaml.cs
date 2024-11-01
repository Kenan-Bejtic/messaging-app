using System;
using System.Threading.Tasks;
using Firebase.Database;
using LoginWithFirebase.ViewModel;

namespace LoginWithFirebase.Views
{
    public partial class FriendInvitationPage : ContentPage
    {
        private readonly FirebaseClient _firebaseClient;

        public FriendInvitationPage(string currentUserInviteCode)
        {
            InitializeComponent();

            
            if (string.IsNullOrEmpty(currentUserInviteCode))
            {
                Console.WriteLine("Error: currentUserInviteCode is null or empty.");
                return;
            }

            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");
            BindingContext = new FriendInvitationViewModel(_firebaseClient, currentUserInviteCode);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var viewModel = BindingContext as FriendInvitationViewModel;
            if (viewModel != null)
            {
                
                Console.WriteLine("Loading pending requests...");
                await viewModel.LoadPendingRequestsAsync();
            }
            else
            {
                Console.WriteLine("Error: BindingContext is not correctly set to FriendInvitationViewModel.");
            }
        }
    }
}
