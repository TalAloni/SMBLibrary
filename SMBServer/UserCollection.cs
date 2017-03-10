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
    public class UserCollection : List<User>
    {
        public void Add(string accountName, string password)
        {
            Add(new User(accountName, password));
        }

        public int IndexOf(string accountName)
        {
            for (int index = 0; index < this.Count; index++)
            {
                if (string.Equals(this[index].AccountName, accountName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return index;
                }
            }
            return -1;
        }

        public string GetUserPassword(string accountName)
        {
            int index = IndexOf(accountName);
            if (index >= 0)
            {
                return this[index].Password;
            }
            return null;
        }

        public List<string> ListUsers()
        {
            List<string> result = new List<string>();
            foreach (User user in this)
            {
                result.Add(user.AccountName);
            }
            return result;
        }
    }
}
