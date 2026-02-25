using System;

namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-dtyp/7a53f60e-e730-4dfe-bbe9-b21b62eb790b">
    /// [MS-DTYP] 2.4.3 - ACCESS_MASK</see>
    /// The bits in positions 16 through 31 are object specific.
    /// </summary>
    [Flags]
    public enum AccessMask : uint
    {
        /// <summary>
        /// Specifies access to delete an object.
        /// </summary>
        DELETE = 0x00010000,

        /// <summary>
        /// Specifies access to read the security descriptor of an object.
        /// </summary>
        READ_CONTROL = 0x00020000,

        /// <summary>
        /// Specifies access to change the discretionary access control list of the security descriptor of an object.
        /// </summary>
        WRITE_DAC = 0x00040000,

        /// <summary>
        /// Specifies access to change the owner of the object as listed in the security descriptor.
        /// </summary>
        WRITE_OWNER = 0x00080000,

        /// <summary>
        /// Specifies access to the object sufficient to synchronize or wait on the object.
        /// </summary>
        SYNCHRONIZE = 0x00100000,

        /// <summary>
        /// When requested, this bit grants the requestor the right to change the SACL of an object. 
        /// </summary>
        ACCESS_SYSTEM_SECURITY = 0x01000000,

        /// <summary>
        /// When requested, this bit grants the requestor the maximum permissions allowed to the object
        /// through the Access Check Algorithm. This bit can only be requested; it cannot be set in an ACE.
        /// </summary>
        MAXIMUM_ALLOWED = 0x02000000,

        /// <summary>
        /// All access to an object is requested.
        /// </summary>
        GENERIC_ALL = 0x10000000,

        /// <summary>
        /// Execute access to an object is requested.
        /// </summary>
        GENERIC_EXECUTE = 0x20000000,

        /// <summary>
        /// Write access to an object is requested.
        /// </summary>
        GENERIC_WRITE = 0x40000000,

        /// <summary>
        /// Read access to an object is requested.
        /// </summary>
        GENERIC_READ = 0x80000000,
    }
}
