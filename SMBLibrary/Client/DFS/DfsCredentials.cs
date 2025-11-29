using System;

namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Holds credentials for DFS client authentication when connecting to referred servers.
    /// </summary>
    public class DfsCredentials
    {
        /// <summary>
        /// Gets the domain name.
        /// </summary>
        public string DomainName { get; private set; }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Initializes a new instance of <see cref="DfsCredentials"/>.
        /// </summary>
        /// <param name="domainName">The domain name.</param>
        /// <param name="userName">The user name.</param>
        /// <param name="password">The password.</param>
        public DfsCredentials(string domainName, string userName, string password)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }

            if (password == null)
            {
                throw new ArgumentNullException("password");
            }

            DomainName = domainName;
            UserName = userName;
            Password = password;
        }
    }
}
