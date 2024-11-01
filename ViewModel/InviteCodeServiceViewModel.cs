using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;

namespace LoginWithFirebase.Services
{
    public class InviteCodeService
    {
        private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private readonly FirebaseClient _firebaseClient;
        private readonly Random _random;

        public InviteCodeService(string firebaseDatabaseUrl)
        {
            _firebaseClient = new FirebaseClient(firebaseDatabaseUrl);
            _random = new Random();
        }

        private string GenerateRandomCode()
        {
            return new string(Enumerable.Range(1, CodeLength)
                .Select(_ => Characters[_random.Next(Characters.Length)])
                .ToArray());
        }

        public async Task<string> GenerateUniqueCodeAsync()
        {
            string code;
            bool isUnique = false;

            do
            {
                code = GenerateRandomCode();
                var existingUsers = await _firebaseClient
                    .Child("users")
                    .OrderBy("inviteCode")
                    .EqualTo(code)
                    .OnceAsync<dynamic>();

                if (!existingUsers.Any())
                {
                    isUnique = true;
                }
            } while (!isUnique);

            return code;
        }

        public async Task AssignInviteCodeAsync(string userId)
        {
            try
            {
                string inviteCode = await GenerateUniqueCodeAsync();

                
                await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .PatchAsync(new { inviteCode });

                
                Console.WriteLine($"Successfully assigned inviteCode: {inviteCode} to userId: {userId}");
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error: {ex.Message}");
                throw; 
            }
        }



    }
}
