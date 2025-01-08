using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using LoginWithFirebase.ViewModel;
using LoginWithFirebase.Views;
using Microsoft.Maui.Controls;

namespace LoginWithFirebase
{

	public partial class SplashScreen : ContentPage
	{

        private FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;


        public SplashScreen(FirebaseAuthClient firebaseAuthClient)
		{
			InitializeComponent();

            _firebaseAuthClient = firebaseAuthClient;
            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");

            StartSplashScreenTimer();
        }


        private async void StartSplashScreenTimer()
        {
            
            await Task.Delay(5000);
            
            Application.Current.MainPage = new NavigationPage(new SignInPage(new SignInViewModel(_firebaseAuthClient)));
        }
    }

}