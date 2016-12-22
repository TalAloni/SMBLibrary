
namespace SMBLibrary.SMB1
{
    public enum FindInformationLevel : ushort
    {
        SMB_INFO_STANDARD = 0x0001,
        SMB_INFO_QUERY_EA_SIZE = 0x0002,
        SMB_INFO_QUERY_EAS_FROM_LIST = 0x0003,
        SMB_FIND_FILE_DIRECTORY_INFO = 0x0101,
        SMB_FIND_FILE_FULL_DIRECTORY_INFO = 0x0102,
        SMB_FIND_FILE_NAMES_INFO = 0x0103,
        SMB_FIND_FILE_BOTH_DIRECTORY_INFO = 0x0104,
    }
}
