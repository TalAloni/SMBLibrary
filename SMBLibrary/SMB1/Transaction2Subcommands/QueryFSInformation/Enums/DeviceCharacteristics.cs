using System;

namespace SMBLibrary.SMB1
{
    [Flags]
    public enum DeviceCharacteristics : uint
    {
        FILE_REMOVABLE_MEDIA = 0x0001,
        FILE_READ_ONLY_DEVICE = 0x0002,
        FILE_FLOPPY_DISKETTE = 0x0004,
        FILE_WRITE_ONCE_MEDIA = 0x0008,
        FILE_REMOTE_DEVICE = 0x0010,
        FILE_DEVICE_IS_MOUNTED = 0x0020,
        FILE_VIRTUAL_VOLUME = 0x0040,
    }
}
