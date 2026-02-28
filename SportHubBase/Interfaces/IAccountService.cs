namespace SportHubBase.Interfaces
{
    public interface IAccountService
    {
        bool Register(string username, string password, out string errorMessage);
        bool Login(string username, string password, out string errorMessage);
    }
}
