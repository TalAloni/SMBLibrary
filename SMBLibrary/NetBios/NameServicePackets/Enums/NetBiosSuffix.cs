namespace SMBLibrary.NetBios
{
    /// <summary>
    /// 16th character suffix for netbios name.
    /// see https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-nbte/6dbf0972-bb15-4f29-afeb-baaae98416ed
    /// </summary>
    public enum NetBiosSuffix : byte
    {
        WorkstationService = 0x00,
        MessengerService = 0x03,
        DomainMasterBrowser = 0x1B,
        MasterBrowser = 0x1D,
        BrowserServiceElections = 0x1E,
        FileServerService = 0x20,
    }
}
