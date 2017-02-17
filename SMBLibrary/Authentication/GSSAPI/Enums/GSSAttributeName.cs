using System;

namespace SMBLibrary.Authentication.GSSAPI
{
    public enum GSSAttributeName
    {
        AccessToken,
        IsAnonymous,
        
        /// <summary>
        /// Permit access to this user via the guest user account if the normal authentication process fails.
        /// </summary>
        IsGuest,
        MachineName,
        SessionKey,
        UserName,
    }
}
