/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.Server
{
    public class ServerPathUtils
    {
        /// <param name="path">UNC path, e.g. '\\192.168.1.1\Shared'</param>
        /// <returns>e.g. \Shared</returns>
        public static string GetRelativeServerPath(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                int index = path.IndexOf('\\', 2);
                if (index > 0)
                {
                    return path.Substring(index);
                }
                else
                {
                    return String.Empty;
                }
            }
            return path;
        }

        /// <param name="path">UNC path, e.g. '\\192.168.1.1\Shared\*'</param>
        /// <returns>e.g. \*</returns>
        public static string GetRelativeSharePath(string path)
        {
            if (path.StartsWith(@"\\"))
            {
                int firstIndex = path.IndexOf('\\', 2);
                int index = path.IndexOf('\\', firstIndex + 1);
                if (index > 0)
                {
                    return path.Substring(index);
                }
                else
                {
                    return path;
                }
            }
            return path;
        }
    }
}
