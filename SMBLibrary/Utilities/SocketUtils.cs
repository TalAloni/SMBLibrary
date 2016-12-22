using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Utilities
{
    public class SocketUtils
    {
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
