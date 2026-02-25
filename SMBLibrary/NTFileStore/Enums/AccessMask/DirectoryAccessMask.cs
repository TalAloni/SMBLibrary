using System;
using static System.Net.WebRequestMethods;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-SMB] 2.2.1.4.2 - Directory_Access_Mask
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/0a5934b1-80f1-4da0-b1bf-5e021c309b71">
    /// [MS-SMB2] 2.2.13.1.2 - Directory_Access_Mask</see>
    /// </summary>
    [Flags]
    public enum DirectoryAccessMask : uint
    {
        /// <summary>
        /// This value indicates the right to enumerate the contents of the directory.
        /// </summary>
        FILE_LIST_DIRECTORY = 0x00000001,

        /// <summary>
        /// This value indicates the right to create a file under the directory.
        /// </summary>
        FILE_ADD_FILE = 0x00000002,

        /// <summary>
        /// This value indicates the right to add a sub-directory under the directory.
        /// </summary>
        FILE_ADD_SUBDIRECTORY = 0x00000004,

        /// <summary>
        /// This value indicates the right to read the extended attributes of the directory.
        /// </summary>
        FILE_READ_EA = 0x00000008,

        /// <summary>
        /// This value indicates the right to write or change the extended attributes of the directory.
        /// </summary>
        FILE_WRITE_EA = 0x00000010,

        /// <summary>
        /// This value indicates the right to traverse this directory if the server enforces traversal checking.
        /// </summary>
        FILE_TRAVERSE = 0x00000020,

        /// <summary>
        /// This value indicates the right to delete the files and directories within this directory.
        /// </summary>
        FILE_DELETE_CHILD = 0x00000040,

        /// <summary>
        /// This value indicates the right to read the attributes of the directory.
        /// </summary>
        FILE_READ_ATTRIBUTES = 0x00000080,

        /// <summary>
        /// This value indicates the right to change the attributes of the directory.
        /// </summary>
        FILE_WRITE_ATTRIBUTES = 0x00000100,

        /// <summary>
        /// This value indicates the right to delete the directory.
        /// </summary>
        DELETE = 0x00010000,

        /// <summary>
        /// This value indicates the right to read the security descriptor for the directory.
        /// </summary>
        READ_CONTROL = 0x00020000,

        /// <summary>
        /// This value indicates the right to change the DACL in the security descriptor for the directory.
        /// </summary>
        WRITE_DAC = 0x00040000,

        /// <summary>
        /// This value indicates the right to change the owner in the security descriptor for the directory.
        /// </summary>
        WRITE_OWNER = 0x00080000,

        /// <summary>
        /// SMB2 clients set this flag to any value. SMB2 servers SHOULD ignore this flag.
        /// </summary>
        SYNCHRONIZE = 0x00100000,

        /// <summary>
        /// This value indicates the right to read or change the SACL in the security descriptor for the directory.
        /// </summary>
        ACCESS_SYSTEM_SECURITY = 0x01000000,

        /// <summary>
        /// This value indicates that the client is requesting an open to the directory with the highest level
        /// of access the client has on this directory. If no access is granted for the client on this directory,
        /// the server MUST fail the open with STATUS_ACCESS_DENIED.
        /// </summary>
        MAXIMUM_ALLOWED = 0x02000000,

        /// <summary>
        /// This value indicates a request for all the access flags that are listed above except
        /// MAXIMUM_ALLOWED and ACCESS_SYSTEM_SECURITY.
        /// </summary>
        GENERIC_ALL = 0x10000000,

        /// <summary>
        /// This value indicates a request for the following access flags listed above:
        /// FILE_READ_ATTRIBUTES | FILE_TRAVERSE | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_EXECUTE = 0x20000000,

        /// <summary>
        /// This value indicates a request for the following access flags listed above:
        /// FILE_ADD_FILE | FILE_ADD_SUBDIRECTORY | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_WRITE = 0x40000000,

        /// <summary>
        /// This value indicates a request for the following access flags listed above:
        /// FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_READ = 0x80000000,
    }
}
