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

namespace SMBLibrary.SMB1
{
    public class FindInformationList : List<FindInformation>
    {
        public FindInformationList()
        {
        }

        public FindInformationList(byte[] buffer, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                FindInformation entry = FindInformation.ReadEntry(buffer, ref offset, informationLevel, isUnicode, returnResumeKeys);
                this.Add(entry);
            }
        }

        public byte[] GetBytes(bool isUnicode)
        {
            for(int index = 0; index < this.Count; index++)
            {
                if (index < this.Count - 1)
                {
                    FindInformation entry = this[index];
                    int entryLength = entry.GetLength(isUnicode);
                    if (entry is FindFileBothDirectoryInfo)
                    {
                        ((FindFileBothDirectoryInfo)entry).NextEntryOffset = (uint)entryLength;
                    }
                    else if (entry is FindFileDirectoryInfo)
                    {
                        ((FindFileDirectoryInfo)entry).NextEntryOffset = (uint)entryLength;
                    }
                    else if (entry is FindFileFullDirectoryInfo)
                    {
                        ((FindFileFullDirectoryInfo)entry).NextEntryOffset = (uint)entryLength;
                    }
                    else if (entry is FindFileNamesInfo)
                    {
                        ((FindFileNamesInfo)entry).NextEntryOffset = (uint)entryLength;
                    }
                }
            }
            int length = GetLength(isUnicode);
            byte[] buffer = new byte[length];
            int offset = 0;
            foreach (FindInformation entry in this)
            {
                entry.WriteBytes(buffer, ref offset, isUnicode);
            }
            return buffer;
        }

        public int GetLength(bool isUnicode)
        {
            int length = 0;
            for (int index = 0; index < this.Count; index++)
            {
                FindInformation entry = this[index];
                int entryLength = entry.GetLength(isUnicode);
                length += entryLength;
            }
            return length;
        }
    }
}
