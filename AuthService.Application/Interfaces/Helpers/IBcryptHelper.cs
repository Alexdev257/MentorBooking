namespace AuthService.Application.Interfaces.Helpers
{
    public interface IBcryptHelper
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }
}
