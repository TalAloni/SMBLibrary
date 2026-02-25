using System;
using static System.Net.WebRequestMethods;

namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-smb2/598f395a-e7a2-4cc8-afb3-ccb30dd2df7c">
    /// [MS-SMB2] 2.2.35 CHANGE_NOTIFY_REQUEST</see>
    /// </summary>
    [Flags]
    public enum NotifyChangeFilter : uint
    {
        /// <summary>
        /// FILE_NOTIFY_CHANGE_FILE_NAME.
        /// The client is notified if a file-name changes.
        /// </summary>
        FileName = 0x0000001,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_DIR_NAME.
        /// The client is notified if a directory name changes.
        /// </summary>
        DirName = 0x0000002,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_ATTRIBUTES.
        /// The client is notified if a file's attributes change.
        /// Possible file attribute values are specified in [MS-FSCC] section 2.6.
        /// </summary>
        Attributes = 0x0000004,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_SIZE.
        /// The client is notified if a file's size changes.
        /// </summary>
        Size = 0x0000008,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_LAST_WRITE.
        /// The client is notified if the last write time of a file changes.
        /// </summary>
        LastWrite = 0x000000010,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_LAST_ACCESS.
        /// The client is notified if the last access time of a file changes.
        /// </summary>
        LastAccess = 0x00000020,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_CREATION.
        /// The client is notified if the creation time of a file changes.
        /// </summary>
        Creation = 0x00000040,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_EA.
        /// The client is notified if a file's extended attributes (EAs) change.
        /// </summary>
        EA = 0x00000080,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_SECURITY.
        /// The client is notified of a file's access control list (ACL) settings change.
        /// </summary>
        Security = 0x00000100,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_STREAM_NAME.
        /// The client is notified if a named stream is added to a file.
        /// </summary>
        StreamName = 0x00000200,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_STREAM_SIZE.
        /// The client is notified if the size of a named stream is changed.
        /// </summary>
        StreamSize = 0x00000400,

        /// <summary>
        /// FILE_NOTIFY_CHANGE_STREAM_WRITE.
        /// The client is notified if a named stream is modified.
        /// </summary>
        StreamWrite = 0x00000800,
    }
}
