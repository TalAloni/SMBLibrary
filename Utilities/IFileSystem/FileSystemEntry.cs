using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities
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
