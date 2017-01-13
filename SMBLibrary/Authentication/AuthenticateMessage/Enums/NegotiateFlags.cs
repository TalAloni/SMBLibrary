using System;

namespace SMBLibrary.Authentication
{
    [Flags]
    public enum NegotiateFlags : uint
    {
        NegotiateUnicode = 0x01, // NTLMSSP_NEGOTIATE_UNICODE
        NegotiateOEM = 0x02, // NTLM_NEGOTIATE_OEM
        RequestTarget = 0x04, // NTLMSSP_REQUEST_TARGET
        NegotiateSign = 0x10, // NTLMSSP_NEGOTIATE_SIGN
        NegotiateSeal = 0x20, // NTLMSSP_NEGOTIATE_SEAL
        NegotiateDatagram = 0x40, // NTLMSSP_NEGOTIATE_DATAGRAM

        /// <summary>
        /// NegotiateLanManagerKey and NegotiateExtendedSecurity are mutually exclusive
        /// If both are set then NegotiateLanManagerKey must be ignored
        /// </summary>
        NegotiateLanManagerKey = 0x80, // NTLMSSP_NEGOTIATE_LM_KEY
        NegotiateNTLMKey = 0x200, // NTLMSSP_NEGOTIATE_NTLM
        //NegotiateNTOnly = 0x400, // Unused, must be clear
        
        /// <summary>
        /// If set, the connection SHOULD be anonymous
        /// </summary>
        NegotiateAnonymous = 0x800,

        NegotiateOEMDomainSupplied = 0x1000, // NTLMSSP_NEGOTIATE_OEM_DOMAIN_SUPPLIED
        NegotiateOEMWorkstationSupplied = 0x2000, // NTLMSSP_NEGOTIATE_OEM_WORKSTATION_SUPPLIED
        NegotiateAlwaysSign = 0x8000, // NTLMSSP_NEGOTIATE_ALWAYS_SIGN
        NegotiateTargetTypeDomain = 0x10000, // NTLMSSP_TARGET_TYPE_DOMAIN
        NegotiateTargetTypeServer = 0x20000, // NTLMSSP_TARGET_TYPE_SERVER
        NegotiateTargetTypeShare = 0x40000, // Unused, must be clear

        /// <summary>
        /// NegotiateLanManagerKey and NegotiateExtendedSecurity are mutually exclusive
        /// If both are set then NegotiateLanManagerKey must be ignored.
        /// NTLM v2 requires this flag to be set.
        /// </summary>
        NegotiateExtendedSecurity = 0x80000, // NTLMSSP_NEGOTIATE_EXTENDED_SESSIONSECURITY
        NegotiateIdentify = 0x100000, // NTLMSSP_NEGOTIATE_IDENTIFY
        RequestNonNTSession = 0x400000, // NTLMSSP_REQUEST_NON_NT_SESSION_KEY
        NegotiateTargetInfo = 0x800000, // NTLMSSP_NEGOTIATE_TARGET_INFO
        NegotiateVersion = 0x2000000, // NTLMSSP_NEGOTIATE_VERSION
        Negotiate128 = 0x20000000, // NTLMSSP_NEGOTIATE_128
        NegotiateKeyExchange = 0x40000000, // NTLMSSP_NEGOTIATE_KEY_EXCH
        Negotiate56 = 0x80000000, // NTLMSSP_NEGOTIATE_56
    }
}
