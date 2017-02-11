/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Utilities;

namespace SMBLibrary.Server
{
    public class AccessRequestArgs : EventArgs
    {
        public string UserName;
        public string Path;
        public FileAccess RequestedAccess;
        public IPEndPoint ClientEndPoint;
        public bool Allow = true;

        public AccessRequestArgs(string userName, string path, FileAccess requestedAccess, IPEndPoint clientEndPoint)
        {
            UserName = userName;
            Path = path;
            RequestedAccess = requestedAccess;
            ClientEndPoint = clientEndPoint;
        }
    }

    public class FileSystemShare : ISMBShare
    {
        private string m_name;
        private INTFileStore m_fileSystem;

        public event EventHandler<AccessRequestArgs> OnAccessRequest;

        public FileSystemShare(string shareName, INTFileStore fileSystem)
        {
            m_name = shareName;
            m_fileSystem = fileSystem;
        }

        public FileSystemShare(string shareName, IFileSystem fileSystem)
        {
            m_name = shareName;
            m_fileSystem = new NTFileSystemAdapter(fileSystem);
        }

        public bool HasReadAccess(string userName, string path, IPEndPoint clientEndPoint)
        {
            return HasAccess(userName, path, FileAccess.Read, clientEndPoint);
        }

        public bool HasWriteAccess(string userName, string path, IPEndPoint clientEndPoint)
        {
            return HasAccess(userName, path, FileAccess.Write, clientEndPoint);
        }

        public bool HasAccess(string userName, string path, FileAccess requestedAccess, IPEndPoint clientEndPoint)
        {
            // To be thread-safe we must capture the delegate reference first
            EventHandler<AccessRequestArgs> handler = OnAccessRequest;
            if (handler != null)
            {
                AccessRequestArgs args = new AccessRequestArgs(userName, path, requestedAccess, clientEndPoint);
                handler(this, args);
                return args.Allow;
            }
            return true;
        }

        public string Name
        {
            get
            {
                return m_name;
            }
        }

        public INTFileStore FileStore
        {
            get
            {
                return m_fileSystem;
            }
        }
    }
}
