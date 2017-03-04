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
        /// Socket will be forcefully closed, all pending data will be ignored, and socket will be deallocated.
        /// </summary>
        public static void ReleaseSocket(Socket socket)
        {
            if (socket != null)
            {
                if (socket.Connected)
                {
                    socket.Shutdown(SocketShutdown.Both);
                    try
                    {
                        socket.Disconnect(false);
                    }
                    catch (SocketException)
                    { }
                }
                socket.Close();
                socket = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
