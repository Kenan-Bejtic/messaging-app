using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using LoginWithFirebase.Model;
using LoginWithFirebase.Views;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Database;

namespace LoginWithFirebase.ViewModel
{
    public partial class SignUpViewModel : BaseViewModel
    {
        private readonly FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseAuthService _authService = new FirebaseAuthService();
        private readonly FirebaseClient _firebaseClient;

        public SignUpViewModel(FirebaseAuthClient firebaseAuthClient)
        {
            _firebaseAuthClient = firebaseAuthClient;
            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");

        }

        [ObservableProperty]
        private SignUpModel _signUpModel = new();

        [RelayCommand]
        private async Task SignUp()
        {
            try
            {
                var signInViewModel = new SignInViewModel(_firebaseAuthClient);

                if (!IsValidEmail(_signUpModel.Email))
                {
                    await Application.Current.MainPage.DisplayAlert("Invalid Email", "Please enter a valid email address.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_signUpModel.Password) || _signUpModel.Password.Length < 8)
                {
                    await Application.Current.MainPage.DisplayAlert("Weak Password", "Password must be at least 8 characters long.", "OK");
                    return;
                }

                if (!Regex.IsMatch(_signUpModel.Password, @"[A-Z]") || !Regex.IsMatch(_signUpModel.Password, @"[0-9]"))
                {
                    await Application.Current.MainPage.DisplayAlert("Weak Password", "Password must contain at least one uppercase letter and one number.", "OK");
                    return;
                }


                var result = await _firebaseAuthClient.CreateUserWithEmailAndPasswordAsync(
                    _signUpModel.Email, _signUpModel.Password, _signUpModel.Username);


                if (result != null && result.User != null)
                {

                    var idToken = await result.User.GetIdTokenAsync(false);


                    await _authService.SendEmailVerification(idToken);

                   
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage = new NavigationPage(new SignInPage(signInViewModel));
                    });




                }
            }
            catch (FirebaseAuthException firebaseEx) when (firebaseEx.Reason == AuthErrorReason.EmailExists)
            {
                await Application.Current.MainPage.DisplayAlert("Email Exists",
                    "This email is already registered. Please sign in or use a different email.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during sign-up: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private bool IsValidEmail(string email)
        {
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }

        [RelayCommand]
        private async Task NavigateSignIn()
        {

            var signInViewModel = new SignInViewModel(_firebaseAuthClient);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new NavigationPage(new SignInPage(signInViewModel));
            });
        }



    }
}

