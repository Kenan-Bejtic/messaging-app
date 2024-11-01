using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.Model;
using LoginWithFirebase.Views;
using System;
using System.Threading.Tasks;

namespace LoginWithFirebase.ViewModel
{
    public partial class SignInViewModel : BaseViewModel
    {
        private readonly FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;

        [ObservableProperty]
        private SignInModel _signInModel = new();

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _rememberMe;

        public SignInViewModel(FirebaseAuthClient firebaseAuthClient)
        {
            _firebaseAuthClient = firebaseAuthClient;
            _firebaseClient = new FirebaseClient("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/");

            Task.Run(async () => await CheckRememberedUser());
        }

        [RelayCommand]
        public async Task SignIn(Page page)
        {
            try
            {
                var result = await _firebaseAuthClient.SignInWithEmailAndPasswordAsync(_signInModel.Email, _signInModel.Password);

                if (!string.IsNullOrWhiteSpace(result?.User?.Info?.Email))
                {
                    var isEmailVerified = result.User.Info.IsEmailVerified;

                    if (isEmailVerified)
                    {
                        var userId = result.User.Uid;

                        
                        Preferences.Set("UserId", userId);

                        var userProfile = await _firebaseClient
                            .Child("users")
                            .Child(userId)
                            .OnceSingleAsync<object>();

                        if (userProfile == null)
                        {
                            var newUserProfile = new
                            {
                                username = "",
                                gender = "",
                                profileCompleted = false
                            };

                            await _firebaseClient
                                .Child("users")
                                .Child(userId)
                                .PutAsync(newUserProfile);
                        }

                        var profileCompleted = await _firebaseClient
                            .Child("users")
                            .Child(userId)
                            .Child("profileCompleted")
                            .OnceSingleAsync<bool>();

                        if (_rememberMe)
                        {
                            Preferences.Set("UserEmail", _signInModel.Email);
                        }
                        else
                        {
                            Preferences.Remove("UserEmail");
                        }

                        if (!profileCompleted)
                        {
                            Application.Current.MainPage = new NavigationPage(new CompleteProfilePage(result.User));
                        }
                        else
                        {
                            Application.Current.MainPage = new NavigationPage(new MainPage(_firebaseAuthClient));
                        }
                    }
                    else
                    {
                        await page.DisplayAlert("Email Not Verified", "Please verify your email before signing in.", "OK");
                    }
                }
            }
            catch (FirebaseAuthException)
            {
                await page.DisplayAlert("Error", "The email or password is incorrect. Please try again.", "OK");
            }
            catch (Exception ex)
            {
                await page.DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private async Task CheckRememberedUser()
        {
            var storedUser = Preferences.Get("UserEmail", null);
            var storedUserId = Preferences.Get("UserId", null);

            if (!string.IsNullOrWhiteSpace(storedUser) && !string.IsNullOrWhiteSpace(storedUserId))
            {
                try
                {
                    var signInMethods = await _firebaseAuthClient.FetchSignInMethodsForEmailAsync(storedUser);

                    if (signInMethods != null && signInMethods.ToString().Length > 0)
                    {
                        Application.Current.MainPage = new NavigationPage(new MainPage(_firebaseAuthClient));
                    }
                }
                catch (Exception)
                {
                    Preferences.Remove("UserEmail");
                    Preferences.Remove("UserId");
                    Console.WriteLine("Unable to auto-sign in. Please log in again.");
                }
            }
        }



        [RelayCommand]
        private async Task NavigateSignUp()
        {
            var signUpViewModel = new SignUpViewModel(_firebaseAuthClient);
            Application.Current.MainPage = new NavigationPage(new SignUpPage(signUpViewModel));
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            string email = await Application.Current.MainPage.DisplayPromptAsync("Forgot Password", "Enter your email to reset password:", "Send", "Cancel", "Email", keyboard: Keyboard.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    await _firebaseAuthClient.ResetEmailPasswordAsync(email);
                    await Application.Current.MainPage.DisplayAlert("Reset Email Sent", "A password reset email has been sent. Please check your inbox.", "OK");
                }
                catch (FirebaseAuthException)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "There was an issue sending the reset email. Please check your email address and try again.", "OK");
                }
                catch (Exception)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "An unexpected error occurred. Please try again.", "OK");
                }
            }
        }
    }
}
