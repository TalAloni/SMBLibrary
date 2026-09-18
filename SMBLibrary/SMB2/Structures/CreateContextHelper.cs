/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 *
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBLibrary.SMB2
{
    /// <summary>
    /// Builds and reads the create contexts defined by [MS-SMB2] 2.2.13.2.
    /// </summary>
    public static class CreateContextHelper
    {
        /// <summary>
        /// [MS-SMB2] 2.2.13.2.7 - SMB2_CREATE_TIMEWARP_TOKEN.
        /// Opens the version of the file that existed at <paramref name="timestamp"/>, which is how a
        /// client reads a shadow copy (previous version). The timestamp must match one of the tokens
        /// the server returned from FSCTL_SRV_ENUMERATE_SNAPSHOTS; a server will not search for the
        /// nearest one.
        /// </summary>
        public static CreateContext CreateTimeWarpToken(DateTime timestamp)
        {
            byte[] data = new byte[8];
            FileTimeHelper.WriteFileTime(data, 0, timestamp);
            return new CreateContext() { Name = CreateContextName.TimeWarpToken, Data = data };
        }

        /// <summary>
        /// Reads the timestamp out of an SMB2_CREATE_TIMEWARP_TOKEN create context.
        /// </summary>
        public static DateTime ReadTimeWarpToken(CreateContext createContext)
        {
            if (createContext == null)
            {
                throw new ArgumentNullException("createContext");
            }

            if (createContext.Data.Length < 8)
            {
                throw new ArgumentException("SMB2_CREATE_TIMEWARP_TOKEN must carry an 8 byte timestamp", "createContext");
            }

            return FileTimeHelper.ReadFileTime(createContext.Data, 0);
        }

        /// <summary>
        /// Returns the first create context with the given name, or null when the list does not
        /// contain one.
        /// </summary>
        public static CreateContext FindByName(List<CreateContext> createContexts, string name)
        {
            if (createContexts == null)
            {
                return null;
            }

            foreach (CreateContext createContext in createContexts)
            {
                if (String.Equals(createContext.Name, name, StringComparison.Ordinal))
                {
                    return createContext;
                }
            }

            return null;
        }
    }
}
