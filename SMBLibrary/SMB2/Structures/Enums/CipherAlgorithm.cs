namespace SMBLibrary.SMB2
{
    public enum CipherAlgorithm : ushort
    {
        Aes128Ccm = 0x0001,
        Aes128Gcm = 0x0002,
        Aes256Ccm = 0x0003,
        Aes256Gcm = 0x0004,
    }
}
