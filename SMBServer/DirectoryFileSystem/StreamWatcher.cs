/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.IO;

namespace SMBServer
{
    /// <summary>
    /// A wrapper for the stream class that notify when the stream is closed
    /// </summary>
    public class StreamWatcher : Stream
    {
        private Stream m_stream;

        public event EventHandler Closed;

        public StreamWatcher(Stream stream)
        {
            m_stream = stream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return m_stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_stream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            m_stream.Close();
            EventHandler handler = Closed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
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

        public override bool CanSeek
        {
            get
            {
                return m_stream.CanSeek;
            }
        }

        public override bool CanRead
        {
            get
            {
                return m_stream.CanRead;
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

        public Stream Stream
        {
            get
            {
                return m_stream;
            }
        }
    }
}
