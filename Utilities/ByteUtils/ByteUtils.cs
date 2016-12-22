using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities
{
    public class ByteUtils
    {
        public static byte[] Concatenate(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Array.Copy(a, 0, result, 0, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }

        public static bool AreByteArraysEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int index = 0; index < array1.Length; index++)
            {
                if (array1[index] != array2[index])
                {
                    return false;
                }
            }

            return true;
        }

        public static long CopyStream(Stream input, Stream output)
        {
            // input may not support seeking, so don't use input.Position
            return CopyStream(input, output, Int64.MaxValue);
        }

        public static long CopyStream(Stream input, Stream output, long count)
        {
            const int MaxBufferSize = 4194304; // 4 MB
            int bufferSize = (int)Math.Min(MaxBufferSize, count);
            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int numberOfBytesToRead = (int)Math.Min(bufferSize, count - totalBytesRead);
                int bytesRead = input.Read(buffer, 0, numberOfBytesToRead);
                totalBytesRead += bytesRead;
                output.Write(buffer, 0, bytesRead);
                if (bytesRead == 0) // no more bytes to read from input stream
                {
                    return totalBytesRead;
                }
            }
            return totalBytesRead;
        }
    }
}
