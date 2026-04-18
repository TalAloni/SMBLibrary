/* Copyright (C) 2023-2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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

        /// <summary>
        /// Used when the client needs to perform login to an additional SMB server,
        /// Only used when connecting to a DFS Namespace Server (DFS root).
        /// </summary>
        void ResetSecurityContext(string serverAddress);
    }
}
