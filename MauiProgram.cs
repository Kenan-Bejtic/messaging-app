using CommunityToolkit.Maui;
using Firebase.Auth;
using Firebase.Auth.Providers;
using LoginWithFirebase.Services;
using LoginWithFirebase.ViewModel;
using LoginWithFirebase.Views;
using Microsoft.Extensions.Logging;

namespace LoginWithFirebase
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif  
            builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyB5dQbIgcUlyWq1w2D_pkIkq4JPPG9mpLo",
                AuthDomain = "razvoj-mobilnih-aplikacija.firebaseapp.com",
                Providers = [new EmailProvider()]
            }));

            //InviteCodeService
            builder.Services.AddSingleton<InviteCodeService>(provider =>
               new InviteCodeService("https://razvoj-mobilnih-aplikacija-default-rtdb.europe-west1.firebasedatabase.app/"));


            
            
            //View model
            builder.Services.AddSingleton<SignInViewModel>();
            builder.Services.AddSingleton<SignUpViewModel>();
            builder.Services.AddSingleton<VerificationViewModel>();


            //Pages
            builder.Services.AddSingleton<SignInPage>();
            builder.Services.AddSingleton<SignUpPage>();
            builder.Services.AddSingleton<VerificationPage>();

            return builder.Build();
        }
    }
}
