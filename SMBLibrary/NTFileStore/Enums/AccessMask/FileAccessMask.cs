using System;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-SMB] 2.2.1.4.1 - File_Pipe_Printer_Access_Mask
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/77b36d0f-6016-458a-a7a0-0f4a72ae1534">
    /// [MS-SMB2] 2.2.13.1.1 - File_Pipe_Printer_Access_Mask</see>
    /// </summary>
    [Flags]
    public enum FileAccessMask : uint
    {
        /// <summary>
        /// This value indicates the right to read data from the file or named pipe.
        /// </summary>
        FILE_READ_DATA = 0x00000001,

        /// <summary>
        /// This value indicates the right to write data into the file or named pipe beyond the end of the file.
        /// </summary>
        FILE_WRITE_DATA = 0x00000002,

        /// <summary>
        /// This value indicates the right to append data into the file or named pipe.
        /// </summary>
        FILE_APPEND_DATA = 0x00000004,

        /// <summary>
        /// This value indicates the right to read the extended attributes of the file or named pipe.
        /// </summary>
        FILE_READ_EA = 0x00000008,

        /// <summary>
        /// This value indicates the right to write or change the extended attributes to the file or named pipe.
        /// </summary>
        FILE_WRITE_EA = 0x00000010,

        /// <summary>
        /// This value indicates the right to execute the file.
        /// </summary>
        FILE_EXECUTE = 0x00000020,

        /// <summary>
        /// This value indicates the right to delete entries within a directory.
        /// </summary>
        FILE_DELETE_CHILD = 0x00000040,

        /// <summary>
        /// This value indicates the right to read the attributes of the file.
        /// </summary>
        FILE_READ_ATTRIBUTES = 0x00000080,

        /// <summary>
        /// This value indicates the right to change the attributes of the file.
        /// </summary>
        FILE_WRITE_ATTRIBUTES = 0x00000100,

        /// <summary>
        /// This value indicates the right to delete the file.
        /// </summary>
        DELETE = 0x00010000,

        /// <summary>
        /// This value indicates the right to read the security descriptor for the file or named pipe.
        /// </summary>
        READ_CONTROL = 0x00020000,

        /// <summary>
        /// This value indicates the right to change the discretionary access control list
        /// (DACL) in the security descriptor for the file or named pipe.
        /// </summary>
        WRITE_DAC = 0x00040000,

        /// <summary>
        /// This value indicates the right to change the owner in the security descriptor for the file or named pipe.
        /// </summary>
        WRITE_OWNER = 0x00080000,

        /// <summary>
        /// SMB2 clients set this flag to any value. SMB2 servers SHOULD ignore this flag.
        /// </summary>
        SYNCHRONIZE = 0x00100000,

        /// <summary>
        /// This value indicates the right to read or change the system access control list (SACL)
        /// in the security descriptor for the file or named pipe.
        /// </summary>
        ACCESS_SYSTEM_SECURITY = 0x01000000,

        /// <summary>
        /// This value indicates that the client is requesting an open to the file with thehighest level
        /// of accessthe client has on this file. If no access is granted for the client on this file,
        /// the server MUST fail the open with STATUS_ACCESS_DENIED.
        /// </summary>
        MAXIMUM_ALLOWED = 0x02000000,

        /// <summary>
        /// This value indicates a request for all the access flags that are previously listed
        /// except MAXIMUM_ALLOWED and ACCESS_SYSTEM_SECURITY.
        /// </summary>
        GENERIC_ALL = 0x10000000,

        /// <summary>
        /// This value indicates a request for the following combination of access flags listed above:
        /// FILE_READ_ATTRIBUTES | FILE_EXECUTE | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_EXECUTE = 0x20000000,

        /// <summary>
        /// This value indicates a request for the following combination of access flags listed above:
        /// FILE_WRITE_DATA | FILE_APPEND_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_WRITE = 0x40000000,

        /// <summary>
        /// This value indicates a request for the following combination of access flags listed above:
        /// FILE_READ_DATA | FILE_READ_ATTRIBUTES | FILE_READ_EA | SYNCHRONIZE | READ_CONTROL.
        /// </summary>
        GENERIC_READ = 0x80000000,
    }
}
