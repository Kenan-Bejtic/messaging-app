using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.ViewModel;
using System.Collections.Generic;

namespace LoginWithFirebase.Views
{
    public partial class CompleteProfilePage : ContentPage
    {
        public CompleteProfilePage(Firebase.Auth.User user)
        {
            InitializeComponent();
            BindingContext = new CompleteProfileViewModel(user.Uid); 
        }

    }
}
