/* Copyright (C) 2023 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.Net;
using System.Net.Sockets;

namespace SMBLibrary.Client
{
    public class IPAddressHelper
    {
        public static IPAddress SelectAddressPreferIPv4(IPAddress[] hostAddresses)
        {
            foreach (IPAddress hostAddress in hostAddresses)
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostAddress;
                }
            }

            return hostAddresses[0];
        }
    }
}
