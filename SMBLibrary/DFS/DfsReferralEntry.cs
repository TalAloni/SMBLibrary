/* Copyright (C) 2014-2025 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System.IO;
using Utilities;

namespace SMBLibrary.DFS
{
    public abstract class DfsReferralEntry
    {
        public abstract byte[] GetBytes();

        public abstract int Length
        {
            get;
        }

        public static DfsReferralEntry ReadEntry(byte[] buffer, ref int offset)
        {
            ushort versionNumber = LittleEndianConverter.ToUInt16(buffer, offset + 0);
            switch (versionNumber)
            {
                case 1:
                    return new DfsReferralEntryV1(buffer, ref offset);
                case 2:
                    return new DfsReferralEntryV2(buffer, ref offset);
                case 3:
                    return new DfsReferralEntryV3(buffer, ref offset);
                case 4:
                    return new DfsReferralEntryV4(buffer, ref offset);
                default:
                    throw new InvalidDataException($"DfsReferralEntry version {versionNumber} is invalid");
            }
        }
    }
}
