/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMBLibrary.Server
{
    internal class OpenFileObject
    {
        public uint TreeID;
        public string ShareName;
        public string Path;
        public object Handle;

        public OpenFileObject(uint treeID, string shareName, string path, object handle)
        {
            TreeID = treeID;
            ShareName = shareName;
            Path = path;
            Handle = handle;
        }
    }
}
