using SportHubBase.Interfaces;
using SportHubBase.Models;

namespace SportHubBase.Services
{
    public class DynamicAccountService : IAccountService
    {
        private readonly SqliteAccountService _sqlite;
        private readonly SqlServerAccountService _sqlServer;

        public DynamicAccountService(SqliteAccountService sqlite, SqlServerAccountService sqlServer)
        {
            _sqlite = sqlite;
            _sqlServer = sqlServer;
        }

        // Выбираем активную базу на лету!
        private IAccountService Current => DatabaseConfig.UseSqlServer ? (IAccountService)_sqlServer : _sqlite;
        public bool Register(string u, string p, string e, string f, string ph, string c, out string err) => Current.Register(u, p, e, f, ph, c, out err);
        public bool Login(string u, string p, out string err) => Current.Login(u, p, out err);
        public bool LoginAndGetAccount(string u, string p, out UserAccount a, out string err) => Current.LoginAndGetAccount(u, p, out a, out err);
        public bool UpdateAccountDetail(int id, string col, string val) => Current.UpdateAccountDetail(id, col, val);
        public UserAccount GetAccount(int id) => Current.GetAccount(id);
        public bool ChangePassword(int id, string oldP, string newP, out string err) => Current.ChangePassword(id, oldP, newP, out err);
        public bool DeleteAccount(int id, out string err) => Current.DeleteAccount(id, out err);
    }
}