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
                    await Application.Current.MainPage.DisplayAlert("Nije validan Email", "Molimo vas da unesete ispravnu email adresu.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_signUpModel.Password) || _signUpModel.Password.Length < 8)
                {
                    await Application.Current.MainPage.DisplayAlert("Slaba šifra", "Šifra mora biti minimalno 8 karaktera.", "OK");
                    return;
                }

                if (!Regex.IsMatch(_signUpModel.Password, @"[A-Z]") || !Regex.IsMatch(_signUpModel.Password, @"[0-9]"))
                {
                    await Application.Current.MainPage.DisplayAlert("Slaba šifra", "Šifra mora sadržavati  najmanje jedno veliko slovo i jedan broj.", "OK");
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
                await Application.Current.MainPage.DisplayAlert("Email već postoji",
                    "Ovaj email je već registovan. Prijavite se ili iskoristite drugi mail.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Greška prilikom registracije: {ex.Message}");
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

