namespace CitaFacil.Services.Security
{
    public interface IPasswordHasher
    {
        string Hash(string plainTextPassword);
        bool Verify(string plainTextPassword, string passwordHash);
    }
}

