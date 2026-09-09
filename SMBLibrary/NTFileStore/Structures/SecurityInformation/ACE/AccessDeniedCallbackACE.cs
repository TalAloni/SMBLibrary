using System;
using Utilities;

namespace SMBLibrary
{
    /// <summary>
    /// ACCESS_DENIED_CALLBACK_ACE
    /// [MS-DTYP] 2.4.4.7
    /// </summary>
    public class AccessDeniedCallbackACE : ACE
    {
        public const int FixedLength = 8;

        public AccessMask Mask;
        public SID Sid;
        public byte[] ApplicationData;

        public AccessDeniedCallbackACE()
        {
            Header = new AceHeader();
            Header.AceType = AceType.ACCESS_DENIED_CALLBACK_ACE_TYPE;
            ApplicationData = new byte[0];
        }

        public AccessDeniedCallbackACE(byte[] buffer, int offset)
        {
            Header = new AceHeader(buffer, offset + 0);
            Mask = (AccessMask)LittleEndianConverter.ToUInt32(buffer, offset + 4);
            int sidOffset = offset + 8;
            Sid = new SID(buffer, sidOffset);

            int sidLength = Sid.Length;
            int appDataOffset = sidOffset + sidLength;
            int appDataSize = Header.AceSize - (appDataOffset - offset);

            if (appDataSize > 0)
            {
                ApplicationData = new byte[appDataSize];
                Array.Copy(buffer, appDataOffset, ApplicationData, 0, appDataSize);
            }
            else
            {
                ApplicationData = new byte[0];
            }
        }

        public override void WriteBytes(byte[] buffer, ref int offset)
        {
            Header.AceSize = (ushort)Length;
            Header.WriteBytes(buffer, ref offset);
            LittleEndianWriter.WriteUInt32(buffer, ref offset, (uint)Mask);
            Sid.WriteBytes(buffer, ref offset);

            if (ApplicationData != null && ApplicationData.Length > 0)
            {
                Array.Copy(ApplicationData, 0, buffer, offset, ApplicationData.Length);
                offset += ApplicationData.Length;
            }
        }

        public override int Length
        {
            get
            {
                return FixedLength + Sid.Length + (ApplicationData?.Length ?? 0);
            }
        }
    }
}
