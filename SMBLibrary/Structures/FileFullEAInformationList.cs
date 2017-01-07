/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-FSCC] FILE_FULL_EA_INFORMATION buffer
    /// </summary>
    public class FileFullEAInformationList : List<FileFullEAInformation>
    {
        public FileFullEAInformationList()
        {
        }

        public FileFullEAInformationList(byte[] buffer, int offset)
        {
            FileFullEAInformation entry = new FileFullEAInformation(buffer, offset);
            this.Add(entry);
            while (entry.NextEntryOffset != 0)
            {
                entry = new FileFullEAInformation(buffer, (int)entry.NextEntryOffset);
                this.Add(entry);
            }
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            // When multiple FILE_FULL_EA_INFORMATION data elements are present in the buffer, each MUST be aligned on a 4-byte boundary
            for (int index = 0; index < this.Count; index++)
            {
                this[index].WriteBytes(buffer, offset);
                offset += this[index].Length;
                if (index < this.Count - 1)
                {
                    int padding = (4 - (offset % 4)) % 4;
                    offset += padding;
                }
            }
        }

        public int Length
        {
            get
            {
                // When multiple FILE_FULL_EA_INFORMATION data elements are present in the buffer, each MUST be aligned on a 4-byte boundary
                int length = 0;
                for(int index = 0; index < this.Count; index++)
                {
                    length += this[index].Length;
                    if (index < this.Count - 1)
                    {
                        int padding = (4 - (length % 4)) % 4;
                        length += padding;
                    }
                }
                return length;
            }
        }
    }
}
