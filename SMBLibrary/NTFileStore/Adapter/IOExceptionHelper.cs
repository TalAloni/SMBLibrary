/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Utilities;

namespace SMBLibrary
{
    public class IOExceptionHelper
    {
        public static ushort GetWin32ErrorCode(IOException ex)
        {
            int hResult = GetExceptionHResult(ex);
            // The Win32 error code is stored in the 16 first bits of the value
            return (ushort)(hResult & 0x0000FFFF);
        }

        public static int GetExceptionHResult(IOException ex)
        {
            PropertyInfo hResult = ex.GetType().GetProperty("HResult", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return (int)hResult.GetValue(ex, null);
        }
    }
}
