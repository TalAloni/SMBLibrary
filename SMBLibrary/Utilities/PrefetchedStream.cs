/* Copyright (C) 2016 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
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
        public const int CacheSize = 1048576; // 1 MB

        private long m_cacheOffset;
        private byte[] m_cache = new byte[0];

        private Stream m_stream;
        private object m_syncLock = new object();

        public PrefetchedStream(Stream stream)
        {
            m_stream = stream;
            new Thread(delegate()
            {
                lock (m_syncLock)
                {
                    m_cacheOffset = 0;
                    m_cache = new byte[CacheSize];
                    int bytesRead = m_stream.Read(m_cache, 0, CacheSize);
                    System.Diagnostics.Debug.Print("[{0}] bytes read {1}", DateTime.Now.ToString("HH:mm:ss:ffff"), bytesRead);
                    this.Position = 0;
                    if (bytesRead < CacheSize)
                    {
                        // EOF, we must trim the response data array
                        m_cache = ByteReader.ReadBytes(m_cache, 0, bytesRead);
                    }
                }
            }).Start();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long position;
            lock (m_syncLock)
            {
                position = this.Position;
                bool isInCache = (position >= m_cacheOffset) && (position + count <= m_cacheOffset + m_cache.Length);
                if (!isInCache)
                {
                    m_cacheOffset = position;
                    int cacheSize = Math.Max(CacheSize, count);
                    m_cache = new byte[cacheSize];
                    int bytesRead = m_stream.Read(m_cache, 0, cacheSize);
                    
                    if (bytesRead < cacheSize)
                    {
                        // EOF, we must trim the response data array
                        m_cache = ByteReader.ReadBytes(m_cache, 0, bytesRead);
                    }
                }
            }

            int offsetInCache = (int)(position - m_cacheOffset);
            int bytesRemained = m_cache.Length - offsetInCache;
            int dataLength = Math.Min(count, bytesRemained);

            Array.Copy(m_cache, offsetInCache, buffer, offset, dataLength);
            lock (m_syncLock)
            {
                this.Position = position + dataLength;
            }
            return dataLength;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_cache = new byte[0];
            m_stream.Write(buffer, offset, count);
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
                return m_stream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return m_stream.Position;
            }
            set
            {
                m_stream.Position = value;
            }
        }

        public override void Flush()
        {
            m_stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_stream.SetLength(value);
        }
    }
}
