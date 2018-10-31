/* Copyright (C) 2014-2018 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBServer
{
    public class ShareSettings
    {
        public string ShareName;
        public string SharePath;
        public List<string> ReadAccess;
        public List<string> WriteAccess;

        public ShareSettings(string shareName, string sharePath, List<string> readAccess, List<string> writeAccess)
        {
            ShareName = shareName;
            SharePath = sharePath;
            ReadAccess = readAccess;
            WriteAccess = writeAccess;
        }
    }
}
