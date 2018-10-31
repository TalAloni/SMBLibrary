using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities
{
    public abstract class FileSystem : IFileSystem
    {
        public abstract FileSystemEntry GetEntry(string path);
        public abstract FileSystemEntry CreateFile(string path);
        public abstract FileSystemEntry CreateDirectory(string path);
        public abstract void Move(string source, string destination);
        public abstract void Delete(string path);
        public abstract List<FileSystemEntry> ListEntriesInDirectory(string path);
        public abstract Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options);
        public abstract void SetAttributes(string path, bool? isHidden, bool? isReadonly, bool? isArchived);
        public abstract void SetDates(string path, DateTime? creationDT, DateTime? lastWriteDT, DateTime? lastAccessDT);

        public List<FileSystemEntry> ListEntriesInRootDirectory()
        {
            return ListEntriesInDirectory(@"\");
        }

        public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return OpenFile(path, mode, access, share, FileOptions.None);
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            const int bufferLength = 1024 * 1024;
            FileSystemEntry sourceFile = GetEntry(sourcePath);
            FileSystemEntry destinationFile = GetEntry(destinationPath);
            if (sourceFile == null | sourceFile.IsDirectory)
            {
                throw new FileNotFoundException();
            }

            if (destinationFile != null && destinationFile.IsDirectory)
            {
                throw new ArgumentException("Destination cannot be a directory");
            }

            if (destinationFile == null)
            {
                destinationFile = CreateFile(destinationPath);
            }
            Stream sourceStream = OpenFile(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, FileOptions.SequentialScan);
            Stream destinationStream = OpenFile(destinationPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, FileOptions.None);
            while (sourceStream.Position < sourceStream.Length)
            {
                int readSize = (int)Math.Max(bufferLength, sourceStream.Length - sourceStream.Position);
                byte[] buffer = new byte[readSize];
                sourceStream.Read(buffer, 0, buffer.Length);
                destinationStream.Write(buffer, 0, buffer.Length);
            }
            sourceStream.Close();
            destinationStream.Close();
        }

        public abstract string Name
        {
            get;
        }

        public abstract long Size
        {
            get;
        }

        public abstract long FreeSpace
        {
            get;
        }

        public abstract bool SupportsNamedStreams
        {
            get;
        }

        public static string GetParentDirectory(string path)
        {
            if (path == String.Empty)
            {
                path = @"\";
            }

            if (!path.StartsWith(@"\"))
            {
                throw new ArgumentException("Invalid path");
            }

            if (path.Length > 1 && path.EndsWith(@"\"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            int separatorIndex = path.LastIndexOf(@"\");
            return path.Substring(0, separatorIndex + 1);
        }

        /// <summary>
        /// Will append a trailing slash to a directory path if not already present
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDirectoryPath(string path)
        {
            if (path.EndsWith(@"\"))
            {
                return path;
            }
            else
            {
                return path + @"\";
            }
        }
    }
}
