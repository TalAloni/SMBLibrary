/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-SMB] 2.2.1.4.1 - File_Pipe_Printer_Access_Mask
    /// </summary>
    [Flags]
    public enum FileAccessMask : uint
    {
        FILE_READ_DATA = 0x00000001,
        FILE_WRITE_DATA = 0x00000002,
        FILE_APPEND_DATA = 0x00000004,
        FILE_READ_EA = 0x00000008,
        FILE_WRITE_EA = 0x00000010,
        FILE_EXECUTE = 0x00000020,
        FILE_READ_ATTRIBUTES = 0x00000080,
        FILE_WRITE_ATTRIBUTES = 0x00000100,
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
        ACCESS_SYSTEM_SECURITY = 0x01000000,
        MAXIMUM_ALLOWED = 0x02000000,
        GENERIC_ALL = 0x20000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_READ = 0x80000000,
    }

    /// <summary>
    /// [MS-SMB] 2.2.1.4.2 - Directory_Access_Mask
    /// </summary>
    [Flags]
    public enum DirectoryAccessMask : uint
    {
        FILE_LIST_DIRECTORY = 0x00000001,
        FILE_ADD_FILE = 0x00000002,
        FILE_ADD_SUBDIRECTORY = 0x00000004,
        FILE_READ_EA = 0x00000008,
        FILE_WRITE_EA = 0x00000010,
        FILE_TRAVERSE = 0x00000020,
        FILE_DELETE_CHILD = 0x00000040,
        FILE_READ_ATTRIBUTES = 0x00000080,
        FILE_WRITE_ATTRIBUTES = 0x00000100,
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
        ACCESS_SYSTEM_SECURITY = 0x01000000,
        MAXIMUM_ALLOWED = 0x02000000,
        GENERIC_ALL = 0x20000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_READ = 0x80000000,
    }

    /// <summary>
    /// [MS-DTYP] 2.4.3 - ACCESS_MASK
    /// </summary>
    public struct AccessMask // uint
    {
        public const int Length = 4;

        public FileAccessMask File;
        public FileAccessMask Directory;

        public AccessMask(byte[] buffer, ref int offset) : this(buffer, offset)
        {
            offset += Length;
        }

        public AccessMask(byte[] buffer, int offset)
        {
            uint value = LittleEndianConverter.ToUInt32(buffer, offset);
            File = (FileAccessMask)value;
            Directory = (FileAccessMask)value;
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            uint value = (uint)(this.File | this.Directory);
            LittleEndianWriter.WriteUInt32(buffer, offset, value);
        }

        public void WriteBytes(byte[] buffer, ref int offset)
        {
            WriteBytes(buffer, offset);
            offset += 4;
        }
    }
}
