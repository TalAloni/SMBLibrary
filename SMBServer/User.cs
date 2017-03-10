/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;

namespace SMBServer
{
    public class User
    {
        public string AccountName;
        public string Password;

        public User(string accountName, string password)
        {
            AccountName = accountName;
            Password = password;
        }
    }
}
