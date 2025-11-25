using BCrypt.Net;

namespace CitaFacil.Services.Security
{
    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string plainTextPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(plainTextPassword);
        }

        public bool Verify(string plainTextPassword, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(plainTextPassword, passwordHash);
        }
    }
}

