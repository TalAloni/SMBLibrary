/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.RPC;
using SMBLibrary.Services;

namespace SMBLibrary.Server
{
    public class NamedPipeShare : List<RemoteService>, ISMBShare
    {
        // A pipe share, as defined by the SMB Protocol, MUST always have the name "IPC$".
        public const string NamedPipeShareName = "IPC$";

        public NamedPipeShare(List<string> shareList)
        {
            this.Add(new ServerService(Environment.MachineName, shareList));
            this.Add(new WorkstationService(Environment.MachineName, Environment.MachineName));
        }

        public Stream OpenPipe(string path)
        {
            // It is possible to have a named pipe that does not use RPC (e.g. MS-WSP),
            // However this is not currently needed by our implementation.
            RemoteService service = GetService(path);
            if (service != null)
            {
                // All instances of a named pipe share the same pipe name, but each instance has its own buffers and handles,
                // and provides a separate conduit for client/server communication.
                return new RPCPipeStream(service);
            }
            return null;
        }

        private RemoteService GetService(string path)
        {
            if (path.StartsWith(@"\"))
            {
                path = path.Substring(1);
            }
            foreach (RemoteService service in this)
            {
                if (String.Equals(path, service.PipeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return service;
                }
            }
            return null;
        }

        public string Name
        {
            get
            {
                return NamedPipeShareName;
            }
        }
    }
}
