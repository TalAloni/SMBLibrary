/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utilities;

namespace SMBLibrary.Win32.Security
{
    public class NetworkAPI
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_0
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public String Username;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct USER_INFO_1
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Username;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Password;
            public uint PasswordAge;
            public uint Priv;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Home_Dir;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            public uint Flags;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string ScriptPath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
        public struct LOCALGROUP_USERS_INFO_0
        {
            public string groupname;
        }

        // NetUserEnum - Obtains a list of all users on local machine or network
        [DllImport("Netapi32.dll")]
        public extern static int NetUserEnum(string servername, int level, int filter, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries, out int resume_handle);

        // NetAPIBufferFree - Used to clear the Network buffer after NetUserEnum
        [DllImport("Netapi32.dll")]
        public extern static int NetApiBufferFree(IntPtr Buffer);

        [DllImport("Netapi32.dll")]
        public extern static int NetUserGetLocalGroups([MarshalAs(UnmanagedType.LPWStr)]string servername,[MarshalAs(UnmanagedType.LPWStr)] string username, int level, int flags, out IntPtr bufptr, int prefmaxlen, out int entriesread, out int totalentries);

        public static List<string> EnumerateGroups(string userName)
        {
            List<string> result = new List<string>();
            int entriesRead;
            int totalEntries;
            IntPtr bufPtr;

            const int Level = 0;
            NetUserGetLocalGroups(null, userName, Level, 0, out bufPtr, 1024, out entriesRead, out totalEntries);

            if (entriesRead > 0)
            {
                LOCALGROUP_USERS_INFO_0[] RetGroups = new LOCALGROUP_USERS_INFO_0[entriesRead];
                IntPtr iter = bufPtr;
                for (int i = 0; i < entriesRead; i++)
                {
                    RetGroups[i] = (LOCALGROUP_USERS_INFO_0)Marshal.PtrToStructure(iter, typeof(LOCALGROUP_USERS_INFO_0));
                    iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(LOCALGROUP_USERS_INFO_0)));
                    result.Add(RetGroups[i].groupname);
                }
                NetApiBufferFree(bufPtr);
            }

            return result;
        }

        public static List<string> EnumerateAllUsers()
        {
            List<string> result = new List<string>();
            int entriesRead;
            int totalEntries;
            int resume;

            IntPtr bufPtr;
            const int Level = 0;
            const int FILTER_NORMAL_ACCOUNT = 2;
            NetUserEnum(null, Level, FILTER_NORMAL_ACCOUNT, out bufPtr, -1, out entriesRead, out totalEntries, out resume);

            if (entriesRead > 0)
            {
                USER_INFO_0[] Users = new USER_INFO_0[entriesRead];
                IntPtr iter = bufPtr;
                for (int i = 0; i < entriesRead; i++)
                {
                    Users[i] = (USER_INFO_0)Marshal.PtrToStructure(iter, typeof(USER_INFO_0));
                    iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(USER_INFO_0)));
                    result.Add(Users[i].Username);
                }
                NetApiBufferFree(bufPtr);
            }
            return result;
        }

        public static List<string> EnumerateEnabledUsers()
        {
            List<string> result = new List<string>();
            int entriesRead;
            int totalEntries;
            int resume;

            IntPtr bufPtr;
            const int Level = 1;
            const int FILTER_NORMAL_ACCOUNT = 2;
            const int UF_ACCOUNTDISABLE = 2;
            NetUserEnum(null, Level, FILTER_NORMAL_ACCOUNT, out bufPtr, -1, out entriesRead, out totalEntries, out resume);

            if (entriesRead > 0)
            {
                USER_INFO_1[] Users = new USER_INFO_1[entriesRead];
                IntPtr iter = bufPtr;
                for (int i = 0; i < entriesRead; i++)
                {
                    Users[i] = (USER_INFO_1)Marshal.PtrToStructure(iter, typeof(USER_INFO_1));
                    iter = new IntPtr(iter.ToInt64() + Marshal.SizeOf(typeof(USER_INFO_1)));
                    if ((Users[i].Flags & UF_ACCOUNTDISABLE) == 0)
                    {
                        result.Add(Users[i].Username);
                    }
                }
                NetApiBufferFree(bufPtr);
            }
            return result;
        }

        public static List<string> EnumerateNetworkUsers()
        {
            List<string> result = new List<string>();
            List<string> users = EnumerateEnabledUsers();
            foreach (string userName in users)
            {
                List<string> groups = EnumerateGroups(userName);
                if (groups.Contains("Users") || groups.Contains("Administrators") || groups.Contains("Guests"))
                {
                    result.Add(userName);
                }
            }
            return result;
        }
    }
}
