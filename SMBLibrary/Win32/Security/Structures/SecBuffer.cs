/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SMBLibrary.Win32.Security
{
    public enum SecBufferType : uint
    {
        SECBUFFER_EMPTY = 0,
        SECBUFFER_DATA = 1,
        SECBUFFER_TOKEN = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecBuffer : IDisposable
    {
        public uint cbBuffer;    // Specifies the size, in bytes, of the buffer pointed to by the pvBuffer member.
        public uint BufferType;
        public IntPtr pvBuffer; // A pointer to a buffer.

        public SecBuffer(int bufferSize)
        {
            cbBuffer = (uint)bufferSize;
            BufferType = (uint)SecBufferType.SECBUFFER_TOKEN;
            pvBuffer = Marshal.AllocHGlobal(bufferSize);
        }

        public SecBuffer(byte[] secBufferBytes)
        {
            cbBuffer = (uint)secBufferBytes.Length;
            BufferType = (uint)SecBufferType.SECBUFFER_TOKEN;
            pvBuffer = Marshal.AllocHGlobal(secBufferBytes.Length);
            Marshal.Copy(secBufferBytes, 0, pvBuffer, secBufferBytes.Length);
        }

        public SecBuffer(byte[] secBufferBytes, SecBufferType bufferType)
        {
            cbBuffer = (uint)secBufferBytes.Length;
            BufferType = (uint)bufferType;
            pvBuffer = Marshal.AllocHGlobal(secBufferBytes.Length);
            Marshal.Copy(secBufferBytes, 0, pvBuffer, secBufferBytes.Length);
        }

        public void Dispose()
        {
            if (pvBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pvBuffer);
                pvBuffer = IntPtr.Zero;
            }
        }

        public byte[] GetBufferBytes()
        {
            byte[] buffer = null;
            if (cbBuffer > 0)
            {
                buffer = new byte[cbBuffer];
                Marshal.Copy(pvBuffer, buffer, 0, (int)cbBuffer);
            }
            return buffer;
        }
    }
}
