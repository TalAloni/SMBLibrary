/* Copyright (C) 2012-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Utilities
{
    public class SocketUtils
    {
        public static void SetKeepAlive(Socket socket, TimeSpan timeout)
        {
            // The default settings when a TCP socket is initialized sets the keep-alive timeout to 2 hours and the keep-alive interval to 1 second.
            SetKeepAlive(socket, true, timeout, TimeSpan.FromSeconds(1));
        }

        /// <param name="timeout">the timeout, in milliseconds, with no activity until the first keep-alive packet is sent</param>
        /// <param name="interval">the interval, in milliseconds, between when successive keep-alive packets are sent if no acknowledgement is received</param>
        public static void SetKeepAlive(Socket socket, bool enable, TimeSpan timeout, TimeSpan interval)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            // https://msdn.microsoft.com/en-us/library/dd877220.aspx
            byte[] tcp_keepalive = new byte[12];
            LittleEndianWriter.WriteUInt32(tcp_keepalive, 0, Convert.ToUInt32(enable));
            LittleEndianWriter.WriteUInt32(tcp_keepalive, 4, (uint)timeout.TotalMilliseconds);
            LittleEndianWriter.WriteUInt32(tcp_keepalive, 8, (uint)interval.TotalMilliseconds);
            socket.IOControl(IOControlCode.KeepAliveValues, tcp_keepalive, null);
        }

        /// <summary>
        /// Socket will be forcefully closed and all pending data will be ignored.
        /// </summary>
        public static void ReleaseSocket(Socket socket)
        {
            if (socket != null)
            {
                if (socket.Connected)
                {
                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Disconnect(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (SocketException)
                    {
                    }
                }
                // Closing socket closes the connection, and Close is a wrapper-method around Dispose.
                socket.Close();
            }
        }
    }
}
