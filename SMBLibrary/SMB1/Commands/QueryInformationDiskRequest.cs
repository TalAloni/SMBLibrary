/* Copyright (C) 2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_COM_QUERY_INFORMATION_DISK Request.
    /// This command is deprecated.
    /// This command is used by Windows 98 SE.
    /// </summary>
    public class QueryInformationDiskRequest : SMB1Command
    {
        public QueryInformationDiskRequest()
        {
        }

        public QueryInformationDiskRequest(byte[] buffer, int offset) : base(buffer, offset, false)
        {
        }

        public override CommandName CommandName
        {
            get
            {
                return CommandName.SMB_COM_QUERY_INFORMATION_DISK;
            }
        }
    }
}
