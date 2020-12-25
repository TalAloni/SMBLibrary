/* Copyright (C) 2014-2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;

namespace DiskAccessLibrary.FileSystems.Abstractions
{
    public class FileSystemEntry
    {
        /// <summary>
        /// Full Path. Directory path should end with a trailing slash.
        /// </summary>
        public string FullName;
        public string Name;
        public bool IsDirectory;
        public ulong Size;
        public DateTime CreationTime;
        public DateTime LastWriteTime;
        public DateTime LastAccessTime;
        public bool IsHidden;
        public bool IsReadonly;
        public bool IsArchived;

        public FileSystemEntry(string fullName, string name, bool isDirectory, ulong size, DateTime creationTime, DateTime lastWriteTime, DateTime lastAccessTime, bool isHidden, bool isReadonly, bool isArchived)
        {
            FullName = fullName;
            Name = name;
            IsDirectory = isDirectory;
            Size = size;
            CreationTime = creationTime;
            LastWriteTime = lastWriteTime;
            LastAccessTime = lastAccessTime;
            IsHidden = isHidden;
            IsReadonly = isHidden;
            IsArchived = isHidden;

            if (isDirectory)
            {
                FullName = FileSystem.GetDirectoryPath(FullName);
            }
        }

        public FileSystemEntry Clone()
        {
            FileSystemEntry clone = (FileSystemEntry)MemberwiseClone();
            return clone;
        }
    }
}
