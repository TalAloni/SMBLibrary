using System;
using System.Collections.Generic;
using System.IO;

namespace Utilities
{
    public interface IFileSystem
    {
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        FileSystemEntry GetEntry(string path);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        FileSystemEntry CreateFile(string path);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        FileSystemEntry CreateDirectory(string path);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        void Move(string source, string destination);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        void Delete(string path);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        List<FileSystemEntry> ListEntriesInDirectory(string path);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        void SetAttributes(string path, bool? isHidden, bool? isReadonly, bool? isArchived);

        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        /// <exception cref="System.IO.IOException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        void SetDates(string path, DateTime? creationDT, DateTime? lastWriteDT, DateTime? lastAccessDT);

        string Name
        {
            get; 
        }

        /// <exception cref="System.IO.IOException"></exception>
        long Size
        {
            get;
        }

        /// <exception cref="System.IO.IOException"></exception>
        long FreeSpace
        {
            get;
        }

        /// <summary>
        /// Indicates support for opening named streams (alternate data streams).
        /// Named streams are opened using the filename:stream syntax.
        /// </summary>
        bool SupportsNamedStreams
        {
            get;
        }
    }
}
