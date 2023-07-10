/* Copyright (C) 2014-2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */

using System;
using System.IO;
#if NETSTANDARD2_0
using System.Buffers;
#endif

namespace SMBLibrary.NetBios
{
    public sealed class NBTConnectionReceiveBuffer : IDisposable
    {
        private byte[] m_buffer;
        private int m_readOffset;
        private int m_bytesInBuffer;
        private int? m_packetLength;

        public NBTConnectionReceiveBuffer() : this(SessionPacket.MaxSessionPacketLength)
        {
        }

        /// <param name="bufferLength">Must be large enough to hold the largest possible NBT packet</param>
        public NBTConnectionReceiveBuffer(int bufferLength)
        {
            if (bufferLength < SessionPacket.MaxSessionPacketLength)
                throw new ArgumentException(
                    "bufferLength must be large enough to hold the largest possible NBT packet");
#if NETSTANDARD2_0 
            m_buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
#else
            m_buffer = new byte[bufferLength];
#endif
        }

        public void IncreaseBufferSize(int bufferLength)
        {
            if (m_buffer.Length >= bufferLength) return;
#if NETSTANDARD2_0
            var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
#else
            var buffer = new byte[bufferLength];
#endif
            if (m_bytesInBuffer > 0)
            {
                Array.Copy(m_buffer, m_readOffset, buffer, 0, m_bytesInBuffer);
                m_readOffset = 0;
            }

#if NETSTANDARD2_0
            ArrayPool<byte>.Shared.Return(m_buffer);
#endif
            m_buffer = buffer;
        }

        public void SetNumberOfBytesReceived(int numberOfBytesReceived)
        {
            m_bytesInBuffer += numberOfBytesReceived;
        }

        public bool HasCompletePacket()
        {
            if (m_bytesInBuffer >= 4)
            {
                if (!m_packetLength.HasValue)
                    m_packetLength = SessionPacket.GetSessionPacketLength(m_buffer, m_readOffset);
                return m_bytesInBuffer >= m_packetLength.Value;
            }

            return false;
        }

        /// <summary>
        /// HasCompletePacket must be called and return true before calling DequeuePacket
        /// </summary>
        /// <exception cref="System.IO.InvalidDataException"></exception>
        public SessionPacket DequeuePacket()
        {
            SessionPacket packet;
            try
            {
                packet = SessionPacket.GetSessionPacket(m_buffer, m_readOffset);
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new InvalidDataException("Invalid NetBIOS session packet", ex);
            }

            RemovePacketBytes();
            return packet;
        }

        public void RemovePacketBytes()
        {
            m_bytesInBuffer -= m_packetLength.GetValueOrDefault(0);
            if (m_bytesInBuffer == 0)
            {
                m_readOffset = 0;
                m_packetLength = null;
            }
            else
            {
                m_readOffset += m_packetLength.GetValueOrDefault(0);
                m_packetLength = null;
                if (!HasCompletePacket())
                {
                    Array.Copy(m_buffer, m_readOffset, m_buffer, 0, m_bytesInBuffer);
                    m_readOffset = 0;
                }
            }
        }

        public byte[] Buffer => m_buffer;

        public int WriteOffset => m_readOffset + m_bytesInBuffer;

        public int BytesInBuffer => m_bytesInBuffer;

        public int AvailableLength => m_buffer.Length - (m_readOffset + m_bytesInBuffer);

        private void ReleaseUnmanagedResources()
        {
#if NETSTANDARD2_0
            ArrayPool<byte>.Shared.Return(m_buffer);
#endif
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~NBTConnectionReceiveBuffer()
        {
            ReleaseUnmanagedResources();
        }
    }
}