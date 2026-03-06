namespace SMBLibrary.Client.DFS
{
    /// <summary>
    /// Identifies the type of DFS referral request per MS-DFSC section 3.1.4.2.
    /// Used by the DFS path resolution algorithm to determine which referral
    /// request type to issue based on path characteristics.
    /// </summary>
    public enum DfsRequestType
    {
        /// <summary>
        /// Domain referral request for a path of the form \\DomainName.
        /// Returns a list of DFS root targets in the domain.
        /// </summary>
        DomainReferral = 0,

        /// <summary>
        /// Domain controller (DC) referral request.
        /// Returns a list of domain controllers for a domain.
        /// </summary>
        DcReferral = 1,

        /// <summary>
        /// Root referral request for a path of the form \\Server\Share.
        /// Returns the root targets for the DFS namespace.
        /// </summary>
        RootReferral = 2,

        /// <summary>
        /// SYSVOL or NETLOGON referral request for domain system volumes.
        /// Handled specially per MS-DFSC section 3.1.4.2 steps 10-11.
        /// </summary>
        SysvolReferral = 3,

        /// <summary>
        /// Link referral request for a DFS folder link path.
        /// Returns the folder targets for the specified link.
        /// </summary>
        LinkReferral = 4
    }
}
