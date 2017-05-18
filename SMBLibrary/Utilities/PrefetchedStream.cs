/* Copyright (C) 2016-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Utilities
{
    public class PrefetchedStream : Stream
    {
        public const int CacheSize = 524288; // 512 KB
        public const int ReadAheadThershold = 65536; // 64 KB

        private long m_cacheOffset;
        private byte[] m_cache = new byte[0];

        private Stream m_stream;

        public PrefetchedStream(Stream stream)
        {
            m_stream = stream;
            if (m_stream.CanRead)
            {
                ScheduleReadAhead();
            }
        }

        private void ScheduleReadAhead()
        {
            new Thread(delegate()
            {
                ReadAhead();
            }).Start();
        }

        private void ReadAhead()
        {
            lock (m_stream)
            {
                long position = this.Position;
                bool isInCache = (position >= m_cacheOffset) && (position < m_cacheOffset + m_cache.Length);
                int bytesAlreadyRead;
                if (isInCache)
                {
                    int offsetInCache = (int)(position - m_cacheOffset);
                    bytesAlreadyRead = m_cache.Length - offsetInCache;
                    byte[] oldCache = m_cache;
                    m_cache = new byte[CacheSize];
                    Array.Copy(oldCache, offsetInCache, m_cache, 0, bytesAlreadyRead);
                    this.Position = position + bytesAlreadyRead;
                }
                else
                {
                    bytesAlreadyRead = 0;
                    m_cache = new byte[CacheSize];
                }
                m_cacheOffset = position;
                int bytesRead = m_stream.Read(m_cache, bytesAlreadyRead, CacheSize - bytesAlreadyRead);
                System.Diagnostics.Debug.Print("[{0}] {1} bytes have been read ahead from offset {2}.", DateTime.Now.ToString("HH:mm:ss:ffff"), bytesRead, position);
                if (bytesAlreadyRead + bytesRead < CacheSize)
                {
                    // EOF, we must trim the response data array
                    m_cache = ByteReader.ReadBytes(m_cache, 0, bytesAlreadyRead + bytesRead);
                }
                this.Position = position;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesCopied;
            lock (m_stream)
            {
                long position = this.Position;
                bool isInCache = (position >= m_cacheOffset) && (position < m_cacheOffset + m_cache.Length);
                if (isInCache)
                {
                    int offsetInCache = (int)(position - m_cacheOffset);
                    int bytesAvailableInCache = m_cache.Length - offsetInCache;
                    bytesCopied = Math.Min(count, bytesAvailableInCache);
                    Array.Copy(m_cache, offsetInCache, buffer, offset, bytesCopied);
                    this.Position = position + bytesCopied;

                    if (bytesCopied < count)
                    {
                        int bytesMissing = count - bytesCopied;
                        int bytesRead = m_stream.Read(buffer, offset + bytesCopied, bytesMissing);
                    }

                    if (offsetInCache + ReadAheadThershold >= m_cache.Length)
                    {
                        ScheduleReadAhead();
                    }
                }
                else
                {
                    bytesCopied = m_stream.Read(buffer, 0, count);
                    ScheduleReadAhead();
                }
            }
            return bytesCopied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (m_stream)
            {
                m_cache = new byte[0];
                m_stream.Write(buffer, offset, count);
            }
        }

        public override void Close()
        {
            lock (m_stream)
            {
                m_stream.Close();
            }
            base.Close();
        }

        public override bool CanRead
        {
            get
            {
                return m_stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return m_stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                lock (m_stream)
                {
                    return m_stream.Length;
                }
            }
        }

        public override long Position
        {
            get
            {
                lock (m_stream)
                {
                    return m_stream.Position;
                }
            }
            set
            {
                lock (m_stream)
                {
                    m_stream.Position = value;
                }
            }
        }

        public override void Flush()
        {
            lock (m_stream)
            {
                m_stream.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            lock (m_stream)
            {
                return m_stream.Seek(offset, origin);
            }
        }

        public override void SetLength(long value)
        {
            lock (m_stream)
            {
                m_stream.SetLength(value);
            }
        }
    }
}
