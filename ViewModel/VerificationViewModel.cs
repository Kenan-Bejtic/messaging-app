using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using System.Threading.Tasks;

namespace LoginWithFirebase.ViewModel
{
    public partial class VerificationViewModel : BaseViewModel
    {
        private readonly FirebaseAuthService _authService;
        private readonly string _sessionInfo;
        private readonly FirebaseAuthClient _firebaseAuthClient;

        [ObservableProperty]
        private string _verificationCode;

        public VerificationViewModel(FirebaseAuthService authService, string sessionInfo, FirebaseAuthClient firebaseAuthClient)
        {
            _authService = authService;
            _sessionInfo = sessionInfo;
            _firebaseAuthClient = firebaseAuthClient;
        }

        

        
        public void NavigateToMainPage(Page page)
        {
            
            Application.Current.MainPage = new NavigationPage(new MainPage(_firebaseAuthClient));
        }
    }
}
