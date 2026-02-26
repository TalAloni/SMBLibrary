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
        AlignedDevice = 0x00000001,            // SSINFO_FLAGS_ALIGNED_DEVICE
        PartitionAlignedOnDevice = 0x00000002, // SSINFO_FLAGS_PARTITION_ALIGNED_ON_DEVICE
        NoSeekPenalty = 0x0000004,             // SSINFO_FLAGS_NO_SEEK_PENALTY
        TrimEnabled = 0x00000008,              // SSINFO_FLAGS_TRIM_ENABLED
    }
}
