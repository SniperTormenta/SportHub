using SportHubBase.Models;

namespace SportHubBase.Interfaces
{
    public interface IAccountService
    {
        bool Register(string username, string password, out string errorMessage);
        bool Login(string username, string password, out string errorMessage);

        /// <summary>
        /// Выполняет вход и возвращает заполненный UserAccount при успехе.
        /// </summary>
        bool LoginAndGetAccount(string username, string password, out UserAccount account, out string errorMessage);
        bool UpdateAccountDetail(int userId, string columnName, string value);
        UserAccount GetAccount(int userId);
    }
}
