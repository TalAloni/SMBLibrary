using System;
using System.IO;
using System.Text;

namespace Utilities
{
    public class BigEndianWriter
    {
        public static void WriteInt16(byte[] buffer, int offset, short value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteInt16(byte[] buffer, ref int offset, short value)
        {
            WriteInt16(buffer, offset, value);
            offset += 2;
        }

        public static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteUInt16(byte[] buffer, ref int offset, ushort value)
        {
            WriteUInt16(buffer, offset, value);
            offset += 2;
        }

        public static void WriteUInt24(byte[] buffer, int offset, uint value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 1, buffer, offset, 3);
        }

        public static void WriteUInt24(byte[] buffer, ref int offset, uint value)
        {
            WriteUInt24(buffer, offset, value);
            offset += 3;
        }

        public static void WriteInt32(byte[] buffer, int offset, int value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteInt32(byte[] buffer, ref int offset, int value)
        {
            WriteInt32(buffer, offset, value);
            offset += 4;
        }

        public static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteUInt32(byte[] buffer, ref int offset, uint value)
        {
            WriteUInt32(buffer, offset, value);
            offset += 4;
        }

        public static void WriteInt64(byte[] buffer, int offset, long value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteInt64(byte[] buffer, ref int offset, long value)
        {
            WriteInt64(buffer, offset, value);
            offset += 8;
        }

        public static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteUInt64(byte[] buffer, ref int offset, ulong value)
        {
            WriteUInt64(buffer, offset, value);
            offset += 8;
        }

        public static void WriteGuidBytes(byte[] buffer, int offset, Guid value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);
        }

        public static void WriteGuidBytes(byte[] buffer, ref int offset, Guid value)
        {
            WriteGuidBytes(buffer, offset, value);
            offset += 16;
        }

        public static void WriteInt16(Stream stream, short value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUInt16(Stream stream, ushort value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUInt24(Stream stream, uint value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 1, 3);
        }

        public static void WriteInt32(Stream stream, int value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUInt32(Stream stream, uint value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteInt64(Stream stream, long value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteUInt64(Stream stream, ulong value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void WriteGuidBytes(Stream stream, Guid value)
        {
            byte[] bytes = BigEndianConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
