using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginWithFirebase.ViewModel
{
    public class FirebaseAuthService
    {
        private readonly string _firebaseApiKey = "AIzaSyB5dQbIgcUlyWq1w2D_pkIkq4JPPG9mpLo";
        private readonly string _firebaseProjectId = "razvoj-mobilnih-aplikacija";

       


        public async Task SendEmailVerification(string idToken)
        {
            var httpClient = new HttpClient();

            var requestBody = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={_firebaseApiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                
                await App.Current.MainPage.DisplayAlert("Verification Email Sent", "Please check your email to verify your account.", "OK");
            }
            else
            {
                
                var errorContent = await response.Content.ReadAsStringAsync();
                await App.Current.MainPage.DisplayAlert("Error", $"Failed to send verification email: {errorContent}", "OK");
            }
        }
       
        
        
    }
    

}
