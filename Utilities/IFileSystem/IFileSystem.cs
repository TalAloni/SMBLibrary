using System;
using System.Collections.Generic;
using System.IO;

namespace Utilities
{
    public interface IFileSystem
    {
        FileSystemEntry GetEntry(string path);
        FileSystemEntry CreateFile(string path);
        FileSystemEntry CreateDirectory(string path);
        void Move(string source, string destination);
        void Delete(string path);
        List<FileSystemEntry> ListEntriesInDirectory(string path);
        Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);
        void SetAttributes(string path, bool? isHidden, bool? isReadonly, bool? isArchived);
        void SetDates(string path, DateTime? creationDT, DateTime? lastWriteDT, DateTime? lastAccessDT);

        string Name
        {
            get; 
        }

        long Size
        {
            get;
        }

        long FreeSpace
        {
            get;
        }
    }
}
