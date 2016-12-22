/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using SMBLibrary.RPC;
using SMBLibrary.Services;

namespace SMBLibrary.Server
{
    public class NamedPipeShare : List<RemoteService>
    {
        public NamedPipeShare(List<string> shareList)
        {
            this.Add(new ServerService(Environment.MachineName, shareList));
            this.Add(new WorkstationService(Environment.MachineName, Environment.MachineName));
        }

        public RemoteService GetService(string path)
        {
            foreach (RemoteService service in this)
            {
                if (String.Equals(path, service.PipeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return service;
                }
            }
            return null;
        }
    }
}
