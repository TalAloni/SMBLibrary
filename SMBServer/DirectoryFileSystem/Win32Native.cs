/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SMBServer
{
    public class Win32Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileTime(SafeFileHandle hFile, ref long lpCreationTime, IntPtr lpLastAccessTime, IntPtr lpLastWriteTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, ref long lpLastAccessTime, IntPtr lpLastWriteTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileTime(SafeFileHandle hFile, IntPtr lpCreationTime, IntPtr lpLastAccessTime, ref long lpLastWriteTime);

        internal static void SetCreationTime(SafeFileHandle hFile, DateTime creationTime)
        {
            long fileTime = creationTime.ToFileTime();
            bool success = SetFileTime(hFile, ref fileTime, IntPtr.Zero, IntPtr.Zero);
            if (!success)
            {
                uint error = (uint)Marshal.GetLastWin32Error();
                throw new IOException("Win32 error: " + error);
            }
        }

        internal static void SetLastAccessTime(SafeFileHandle hFile, DateTime lastAccessTime)
        {
            long fileTime = lastAccessTime.ToFileTime();
            bool success = SetFileTime(hFile, IntPtr.Zero, ref fileTime, IntPtr.Zero);
            if (!success)
            {
                uint error = (uint)Marshal.GetLastWin32Error();
                throw new IOException("Win32 error: " + error);
            }
        }

        internal static void SetLastWriteTime(SafeFileHandle hFile, DateTime lastWriteTime)
        {
            long fileTime = lastWriteTime.ToFileTime();
            bool success = SetFileTime(hFile, IntPtr.Zero, IntPtr.Zero, ref fileTime);
            if (!success)
            {
                uint error = (uint)Marshal.GetLastWin32Error();
                throw new IOException("Win32 error: " + error);
            }
        }
    }
}
