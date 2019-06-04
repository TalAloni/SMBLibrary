/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMBLibrary.Win32
{
    public class ProcessHelper
    {
        private static bool? m_is64BitProcess;
        private static bool? m_isWow64Process;

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        public static bool IsWow64Process(Process process)
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                bool retVal;
                if (!IsWow64Process(process.Handle, out retVal))
                {
                    return false;
                }
                return retVal;
            }
            else
            {
                return false;
            }
        }

        public static bool IsWow64Process()
        {
            if (!m_isWow64Process.HasValue)
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    m_isWow64Process = IsWow64Process(process);
                }
            }
            return m_isWow64Process.Value;
        }

        public static bool Is64BitProcess
        {
            get
            {
                if (!m_is64BitProcess.HasValue)
                {
                    m_is64BitProcess = (IntPtr.Size == 8);
                }
                return m_is64BitProcess.Value;
            }
        }

        public static bool Is64BitOperatingSystem
        {
            get
            {
                return Is64BitProcess || IsWow64Process();
            }
        }
    }
}
