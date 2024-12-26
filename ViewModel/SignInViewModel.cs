using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LoginWithFirebase.Model;
using LoginWithFirebase.Views;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; 
using Microsoft.Maui.Storage; 

namespace LoginWithFirebase.ViewModel
{
    public partial class SignInViewModel : BaseViewModel
    {
        private readonly FirebaseAuthClient _firebaseAuthClient;
        private readonly FirebaseClient _firebaseClient;

        [ObservableProperty]
        private SignInModel _signInModel = new();

        [ObservableProperty]
        private bool _rememberMe;

        private const string GenericErrorMessage = "Došlo je do greške, provjerite informacije";


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
                if (_signInModel == null || string.IsNullOrWhiteSpace(_signInModel.Email) || string.IsNullOrWhiteSpace(_signInModel.Password))
                {
                    await HandleError(page, "Email or password is missing. Please provide valid credentials.");
                    return;
                }

                var result = await _firebaseAuthClient.SignInWithEmailAndPasswordAsync(_signInModel.Email, _signInModel.Password);

                if (result == null)
                {
                    await HandleError(page, "Sign-in result is null. Unable to authenticate.");
                    return;
                }

                if (result.User == null || result.User.Info == null)
                {
                    await HandleError(page, "User information is missing. Unable to retrieve user details.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.User.Info.Email))
                {
                    await HandleError(page, "Email information is not available for this user.");
                    return;
                }

                var isEmailVerified = result.User.Info.IsEmailVerified;

                if (!isEmailVerified)
                {
                    await HandleError(page, "Your email is not verified. Please verify your email before signing in.");
                    return;
                }

                var userId = result.User.Uid;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    await HandleError(page, "User ID is missing. Unable to proceed.");
                    return;
                }

                Preferences.Set("UserId", userId);

                var userProfile = await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .OnceSingleAsync<object>();

                if (userProfile == null)
                {
                    Console.WriteLine("User profile not found. Creating a new profile.");
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

                var profileCompleted = false;
                try
                {
                    profileCompleted = await _firebaseClient
                        .Child("users")
                        .Child(userId)
                        .Child("profileCompleted")
                        .OnceSingleAsync<bool>();
                }
                catch (Exception ex)
                {
                    await HandleError(page, $"Unable to fetch profile completion status: {ex.Message}");
                    Console.WriteLine($"Error fetching profile completion status: {ex}");
                    return;
                }

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
                    Console.WriteLine("Navigating to CompleteProfilePage.");
                    Application.Current.MainPage = new NavigationPage(new CompleteProfilePage(result.User));
                }
                else
                {
                    Console.WriteLine("Navigating to MainPage.");
                    Application.Current.MainPage = new NavigationPage(new MainPage(_firebaseAuthClient));
                }
            }
            catch (FirebaseAuthException ex)
            {
                await HandleError(page, "The email or password is incorrect. Please try again.");
                Console.WriteLine($"FirebaseAuthException: {ex.Reason} | {ex.Message}");
            }
            catch (NullReferenceException ex)
            {
                await HandleError(page, "A null reference error occurred. Please ensure all required data is provided.");
                Console.WriteLine($"NullReferenceException: {ex.Message} | StackTrace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                await HandleError(page, "An unexpected error occurred. Please try again.");
                Console.WriteLine($"Unexpected Exception: {ex.Message} | StackTrace: {ex.StackTrace}");
            }
        }


        private async Task HandleError(Page page, string message)
        {
            try
            {
               
                var displayPage = page ?? Application.Current.MainPage;

                if (displayPage == null)
                {
                    Console.WriteLine("Error: Unable to display alert because both the Page object and MainPage are null.");
                    Console.WriteLine($"Original Error Message: {message}");
                    return;
                }

                
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Console.WriteLine($"Error: {message}");
                    await displayPage.DisplayAlert("Error", message, "OK");
                });
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Critical Error: Unable to display alert. Original Error: {message} | DisplayAlert Exception: {ex.Message}");
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

                    if (signInMethods != null )
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            Application.Current.MainPage =
                                new NavigationPage(new MainPage(_firebaseAuthClient));
                        });
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
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Application.Current.MainPage = new NavigationPage(new SignUpPage(signUpViewModel));
            });
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            string email = await Application.Current.MainPage.DisplayPromptAsync(
                "Forgot Password",
                "Enter your email to reset password:",
                "Send",
                "Cancel",
                "Email",
                keyboard: Keyboard.Email);

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    await _firebaseAuthClient.ResetEmailPasswordAsync(email);
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Reset Email Sent",
                            "A password reset email has been sent. Please check your inbox.",
                            "OK");
                    });
                }
                catch (FirebaseAuthException)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Error",
                            "There was an issue sending the reset email. Please check your email address and try again.",
                            "OK");
                    });
                }
                catch (Exception)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Error",
                            "An unexpected error occurred. Please try again.",
                            "OK");
                    });
                }
            }
        }
    }
}
