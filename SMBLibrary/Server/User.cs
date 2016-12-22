using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.Server
{
    public class User
    {
        public string AccountName;
        public string Password;

        public User(string accountName, string password)
        {
            AccountName = accountName;
            Password = password;
        }
    }
}
