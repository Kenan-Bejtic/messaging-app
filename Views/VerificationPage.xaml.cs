using Firebase.Auth;
using LoginWithFirebase.ViewModel;

namespace LoginWithFirebase.Views
{
    public partial class VerificationPage : ContentPage
    {
        private readonly VerificationViewModel _viewModel;
        private readonly FirebaseAuthClient _firebaseAuthClient;

        public VerificationPage(VerificationViewModel viewModel, FirebaseAuthClient firebaseAuthClient)
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
            _firebaseAuthClient = firebaseAuthClient;
        }

        
    }
}
