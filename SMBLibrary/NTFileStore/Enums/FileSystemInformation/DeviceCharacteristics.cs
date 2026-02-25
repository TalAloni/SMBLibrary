using System;

namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/616b66d5-b335-4e1c-8f87-b4a55e8d3e4a">
    /// [MS-FSCC] 2.5.10 - FileFsDeviceInformation</see>
    /// </summary>
    [Flags]
    public enum DeviceCharacteristics : uint
    {
        /// <summary>
        /// FILE_REMOVABLE_MEDIA.
        /// Indicates that the storage device supports removable media.
        /// Notice that this characteristic indicates removable media, not a removable device.
        /// For example, drivers for DVD-ROM devices specify this characteristic,
        /// but drivers for USB flash disks do not.
        /// </summary>
        RemovableMedia = 0x0001,

        /// <summary>
        /// FILE_READ_ONLY_DEVICE.
        /// Indicates that the device cannot be written to.
        /// </summary>
        ReadOnlyDevice = 0x0002,

        /// <summary>
        /// FILE_FLOPPY_DISKETTE.
        /// Indicates that the device is a floppy disk device.
        /// </summary>
        FloppyDiskette = 0x0004,

        /// <summary>
        /// FILE_WRITE_ONCE_MEDIA.
        /// Indicates that the device supports write-once media.
        /// </summary>
        WriteOnceMedia = 0x0008,

        /// <summary>
        /// FILE_REMOTE_DEVICE.
        /// Indicates that the volume is for a remote file system like SMB or CIFS.
        /// </summary>
        RemoteDevice = 0x0010,

        /// <summary>
        /// FILE_DEVICE_IS_MOUNTED.
        /// Indicates that a file system is mounted on the device.
        /// </summary>
        IsMounted = 0x0020,

        /// <summary>
        /// FILE_VIRTUAL_VOLUME.
        /// Indicates that the volume does not directly reside on storage media
        /// but resides on some other type of media (memory for example).
        /// </summary>
        VirtualVolume = 0x0040,

        /// <summary>
        /// FILE_DEVICE_SECURE_OPEN
        /// By default, volumes do not check the ACL associated with the volume,
        /// but instead use the ACLs associated with individual files on the volume.
        /// When this flag is set the volume ACL is also checked.
        /// </summary>
        SecureOpen = 0x0100,

        /// <summary>
        /// FILE_CHARACTERISTIC_TS_DEVICE.
        /// Indicates that the device object is part of a Terminal Services device stack.
        /// </summary>
        TerminalServicesDevice = 0x1000,

        /// <summary>
        /// FILE_CHARACTERISTIC_WEBDAV_DEVICE.
        /// Indicates that a web-based Distributed Authoring and Versioning (WebDAV) file system is mounted on the device.
        /// </summary>
        WebDAVDevice = 0x2000,

        /// <summary>
        /// FILE_PORTABLE_DEVICE.
        /// Indicates that the given device resides on a portable bus like USB or Firewire
        /// and that the entire device (not just the media) can be removed from the system.
        /// </summary>
        PortableDevice = 0x4000,

        /// <summary>
        /// FILE_DEVICE_ALLOW_APPCONTAINER_TRAVERSAL.
        /// The IO Manager normally performs a full security check for traverse access on every file open when the client is an appcontainer.
        /// Setting of this flag bypasses this enforced traverse access check if the client token already has traverse privileges.
        /// </summary>
        AllowAppContainerTraversal = 0x20000,
    }
}
