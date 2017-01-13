/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using SMBLibrary.RPC;

namespace SMBLibrary.Services
{
    public class RPCPipeStream : Stream
    {
        private RemoteService m_service;
        private MemoryStream m_outputStream;

        public RPCPipeStream(RemoteService service)
        {
            m_service = service;
            m_outputStream = new MemoryStream();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_outputStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            int lengthOfPDUs = 0;
            do
            {
                RPCPDU rpcRequest = RPCPDU.GetPDU(buffer, offset);
                lengthOfPDUs += rpcRequest.FragmentLength;
                RPCPDU rpcReply = RemoteServiceHelper.GetRPCReply(rpcRequest, m_service);
                byte[] replyData = rpcReply.GetBytes();
                Append(replyData);
            }
            while (lengthOfPDUs < count);
        }

        private void Append(byte[] buffer)
        {
            long position = m_outputStream.Position;
            m_outputStream.Position = m_outputStream.Length;
            m_outputStream.Write(buffer, 0, buffer.Length);
            m_outputStream.Seek(position, SeekOrigin.Begin);
        }

        public override void Flush()
        {
        }

        public override void Close()
        {
            m_outputStream.Close();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanRead
        {
            get
            {
                return m_outputStream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_outputStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                // Stream.Length only works on Stream implementations where seeking is available.
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
