using Firebase.Auth;
using Firebase.Auth.Providers;
using LoginWithFirebase.ViewModel;
using LoginWithFirebase.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LoginWithFirebase
{
    public partial class App : Application
    {
        private FirebaseAuthClient _firebaseAuthClient;

        public App()
        {
            InitializeComponent();

            
            var config = new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyB5dQbIgcUlyWq1w2D_pkIkq4JPPG9mpLo",
                AuthDomain = "razvoj-mobilnih-aplikacija.firebaseapp.com",
                Providers = [new EmailProvider()]
            };

            _firebaseAuthClient = new FirebaseAuthClient(config);


            
            MainPage = new NavigationPage(new SplashScreen(_firebaseAuthClient));
        }

        public FirebaseAuthClient GetFirebaseAuthClient()
        {
            return _firebaseAuthClient;
        }

    }

}
