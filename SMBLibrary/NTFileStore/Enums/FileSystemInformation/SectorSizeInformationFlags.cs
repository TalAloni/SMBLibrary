using System;

namespace SMBLibrary
{
    /// <summary>
    /// <see href="https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-fscc/3e75d97f-1d0b-4e47-b435-73c513837a57">
    /// [MS-FSCC] 2.5.7 - FileFsSectorSizeInformation</see>
    /// </summary>
    [Flags]
    public enum SectorSizeInformationFlags : uint
    {
        /// <summary>
        /// SSINFO_FLAGS_ALIGNED_DEVICE.
        /// When set, this flag indicates that the first physical sector of the device is aligned
        /// with the first logical sector. When not set, the first physical sector of the device
        /// is misaligned with the first logical sector.
        /// </summary>
        AlignedDevice = 0x00000001,

        /// <summary>
        /// SSINFO_FLAGS_PARTITION_ALIGNED_ON_DEVICE.
        /// When set, this flag indicates that the partition is aligned to physical sector boundaries on the storage device.
        /// </summary>
        PartitionAlignedOnDevice = 0x00000002,

        /// <summary>
        /// SSINFO_FLAGS_NO_SEEK_PENALTY.
        /// When set, the device reports that it does not incur a seek penalty (this typically
        /// indicates that the device does not have rotating media, such as flash-based disks). 
        /// </summary>
        NoSeekPenalty = 0x0000004,

        /// <summary>
        /// SSINFO_FLAGS_TRIM_ENABLED.
        /// When set, the device supports TRIM operations, either T13 (ATA) TRIM or T10 (SCSI/SAS) UNMAP.
        /// </summary>
        TrimEnabled = 0x00000008,
    }
}
