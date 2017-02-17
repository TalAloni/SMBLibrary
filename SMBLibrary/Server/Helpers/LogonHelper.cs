/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using Utilities;

namespace SMBLibrary.Server
{
    public class LogonHelper
    {
        public static NTStatus ToNTStatus(Win32Error errorCode)
        {
            switch (errorCode)
            {
                case Win32Error.ERROR_ACCOUNT_RESTRICTION:
                    return NTStatus.STATUS_ACCOUNT_RESTRICTION;
                case Win32Error.ERROR_INVALID_LOGON_HOURS:
                    return NTStatus.STATUS_INVALID_LOGON_HOURS;
                case Win32Error.ERROR_INVALID_WORKSTATION:
                    return NTStatus.STATUS_INVALID_WORKSTATION;
                case Win32Error.ERROR_PASSWORD_EXPIRED:
                    return NTStatus.STATUS_PASSWORD_EXPIRED;
                case Win32Error.ERROR_ACCOUNT_DISABLED:
                    return NTStatus.STATUS_ACCOUNT_DISABLED;
                case Win32Error.ERROR_LOGON_TYPE_NOT_GRANTED:
                    return NTStatus.STATUS_LOGON_TYPE_NOT_GRANTED;
                case Win32Error.ERROR_ACCOUNT_EXPIRED:
                    return NTStatus.STATUS_ACCOUNT_EXPIRED;
                case Win32Error.ERROR_PASSWORD_MUST_CHANGE:
                    return NTStatus.STATUS_PASSWORD_MUST_CHANGE;
                case Win32Error.ERROR_ACCOUNT_LOCKED_OUT:
                    return NTStatus.STATUS_ACCOUNT_LOCKED_OUT;    
                default:
                    return NTStatus.STATUS_LOGON_FAILURE;
            }
        }
    }
}
