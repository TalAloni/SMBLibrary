
namespace SMBLibrary.SMB1
{
    public enum CreateDisposition : uint
    {
        /// <summary>
        /// If the file already exists, it SHOULD be superseded (overwritten).
        /// If it does not already exist, then it SHOULD be created.
        /// </summary>
        FILE_SUPERSEDE = 0x0000,

        /// <summary>
        /// If the file already exists, it SHOULD be opened rather than created.
        /// If the file does not already exist, the operation MUST fail.
        /// </summary>
        FILE_OPEN = 0x0001,
        
        /// <summary>
        /// If the file already exists, the operation MUST fail.
        /// If the file does not already exist, it SHOULD be created.
        /// </summary>
        FILE_CREATE = 0x0002,
        
        /// <summary>
        /// If the file already exists, it SHOULD be opened.
        /// If the file does not already exist, then it SHOULD be created.
        /// This value is equivalent to (FILE_OPEN | FILE_CREATE).
        /// </summary>
        FILE_OPEN_IF = 0x0003,

        /// <summary>
        /// If the file already exists, it SHOULD be opened and truncated.
        /// If the file does not already exist, the operation MUST fail.
        /// The client MUST open the file with at least GENERIC_WRITE access for the command to succeed.
        /// </summary>
        FILE_OVERWRITE = 0x0004,
        
        /// <summary>
        /// If the file already exists, it SHOULD be opened and truncated.
        /// If the file does not already exist, it SHOULD be created.
        /// The client MUST open the file with at least GENERIC_WRITE access.
        /// </summary>
        FILE_OVERWRITE_IF = 0x0005,
    }
}
