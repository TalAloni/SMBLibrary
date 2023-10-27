/* Copyright (C) 2023-2023 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
namespace SMBLibrary.Client.Authentication
{
    public interface IAuthenticationClient
    {
        /// <returns>Credentials blob or null if security blob is invalid</returns>
        byte[] InitializeSecurityContext(byte[] securityBlob);

        byte[] GetSessionKey();
    }
}
