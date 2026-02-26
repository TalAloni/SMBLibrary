using System;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-FSCC] 2.5.10 - FileFsDeviceInformation
    /// </summary>
    [Flags]
    public enum DeviceCharacteristics : uint
    {
        RemovableMedia = 0x0001,                // FILE_REMOVABLE_MEDIA
        ReadOnlyDevice = 0x0002,                // FILE_READ_ONLY_DEVICE
        FloppyDiskette = 0x0004,                // FILE_FLOPPY_DISKETTE
        WriteOnceMedia = 0x0008,                // FILE_WRITE_ONCE_MEDIA
        RemoteDevice = 0x0010,                  // FILE_REMOTE_DEVICE
        IsMounted = 0x0020,                     // FILE_DEVICE_IS_MOUNTED
        VirtualVolume = 0x0040,                 // FILE_VIRTUAL_VOLUME
        SecureOpen = 0x0100,                    // FILE_DEVICE_SECURE_OPEN
        TerminalServicesDevice = 0x1000,        // FILE_CHARACTERISTIC_TS_DEVICE
        WebDAVDevice = 0x2000,                  // FILE_CHARACTERISTIC_WEBDAV_DEVICE
        PortableDevice = 0x4000,                // FILE_PORTABLE_DEVICE
        AllowAppContainerTraversal = 0x20000,   // FILE_DEVICE_ALLOW_APPCONTAINER_TRAVERSAL
    }
}
