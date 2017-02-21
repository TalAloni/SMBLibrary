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
        public const uint NERR_Success = 0;
        public const uint NERR_UserNotFound = 2221;

        public const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;

        public const uint FILTER_NORMAL_ACCOUNT = 2;

        public const uint UF_ACCOUNTDISABLE = 2;

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

        // NetAPIBufferFree - Used to free buffer allocation after NetUserEnum / NetUserGetInfo / NetUserGetLocalGroups
        [DllImport("Netapi32.dll")]
        public extern static uint NetApiBufferFree(IntPtr buffer);

        // NetUserEnum - Obtains a list of all users on local machine or network
        [DllImport("Netapi32.dll")]
        public extern static uint NetUserEnum([MarshalAs(UnmanagedType.LPWStr)]string servername, uint level, uint filter, out IntPtr bufPtr, uint prefmaxlen, out uint entriesread, out uint totalentries, out uint resume_handle);

        // NetUserGetInfo - Retrieves information about a particular user account on a server
        [DllImport("Netapi32.dll")]
        public extern static uint NetUserGetInfo([MarshalAs(UnmanagedType.LPWStr)]string servername, [MarshalAs(UnmanagedType.LPWStr)]string userName, uint level, out IntPtr bufPtr);

        [DllImport("Netapi32.dll")]
        public extern static uint NetUserGetLocalGroups([MarshalAs(UnmanagedType.LPWStr)]string servername,[MarshalAs(UnmanagedType.LPWStr)] string username, uint level, uint flags, out IntPtr bufPtr, uint prefmaxlen, out uint entriesread, out uint totalentries);

        public static List<string> EnumerateGroups(string userName)
        {
            List<string> result = new List<string>();
            uint entriesRead;
            uint totalEntries;
            IntPtr bufPtr;

            uint level = 0;
            uint status = NetUserGetLocalGroups(null, userName, level, 0, out bufPtr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries);
            if (status != NERR_Success)
            {
                throw new Exception("NetUserGetLocalGroups failed, Error code: " + status.ToString());
            }

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
            uint entriesRead;
            uint totalEntries;
            uint resume;

            IntPtr bufPtr;
            uint level = 0;
            uint status = NetUserEnum(null, level, FILTER_NORMAL_ACCOUNT, out bufPtr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, out resume);
            if (status != NERR_Success)
            {
                throw new Exception("NetUserEnum failed, Error code: " + status.ToString());
            }

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
            uint entriesRead;
            uint totalEntries;
            uint resume;

            IntPtr bufPtr;
            uint level = 1;
            uint status = NetUserEnum(null, level, FILTER_NORMAL_ACCOUNT, out bufPtr, MAX_PREFERRED_LENGTH, out entriesRead, out totalEntries, out resume);
            if (status != NERR_Success)
            {
                throw new Exception("NetUserEnum failed, Error code: " + status.ToString());
            }

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

        public static bool IsUserExists(string userName)
        {
            uint level = 0;
            IntPtr bufPtr;
            uint result = NetUserGetInfo(null, userName, level, out bufPtr);
            if (result == NERR_Success)
            {
                NetApiBufferFree(bufPtr);
                return true;
            }
            else if (result == NERR_UserNotFound)
            {
                return false;
            }
            else
            {
                throw new Exception("NetUserGetInfo failed, Error code: " + result.ToString());
            }
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
