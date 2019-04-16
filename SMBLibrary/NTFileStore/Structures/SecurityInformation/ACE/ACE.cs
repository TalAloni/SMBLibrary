/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary
{
    /// <summary>
    /// [MS-DTYP] ACE (Access Control Entry)
    /// </summary>
    public abstract class ACE
    {
        public abstract void WriteBytes(byte[] buffer, ref int offset);

        public abstract int Length
        {
            get;
        }

        public static ACE GetAce(byte[] buffer, int offset) {
            AceType aceType = (AceType)ByteReader.ReadByte(buffer, offset + 0);
            ACE ace;
            switch (aceType) {
                case AceType.ACCESS_ALLOWED_ACE_TYPE:
                case AceType.SYSTEM_AUDIT_ACE_TYPE:
                case AceType.SYSTEM_SCOPED_POLICY_ID_ACE_TYPE:
                case AceType.ACCESS_DENIED_ACE_TYPE:
                case AceType.SYSTEM_MANDATORY_LABEL_ACE_TYPE:
                    ace = new AceType1(buffer, offset);
                    break;
                //case AceType.ACCESS_ALLOWED_CALLBACK_ACE_TYPE:
                //  ace = AceType3.read(buffer, offset, startPos);
                //  break;
                //case AceType.ACCESS_ALLOWED_CALLBACK_OBJECT_ACE_TYPE:
                //  ace = AceType4.read(buffer, offset, startPos);
                //  break;
                //case AceType.ACCESS_ALLOWED_OBJECT_ACE_TYPE:
                //  ace = AceType2.read(buffer, offset, startPos);
                //  break;
                //case AceType.ACCESS_DENIED_CALLBACK_ACE_TYPE:
                //  ace = AceType3.read(buffer, offset, startPos);
                //  break;
                //case AceType.ACCESS_DENIED_CALLBACK_OBJECT_ACE_TYPE:
                //  ace = AceType4.read(buffer, offset, startPos);
                //  break;
                //case AceType.ACCESS_DENIED_OBJECT_ACE_TYPE:
                //  ace = AceType2.read(buffer, offset, startPos);
                //  break;
                //case AceType.SYSTEM_AUDIT_CALLBACK_ACE_TYPE:
                //  ace = AceType3.read(buffer, offset, startPos);
                //  break;
                //case SYSTEM_AUDIT_CALLBACK_OBJECT_ACE_TYPE:
                //  ace = AceType4.read(buffer, offset, startPos);
                //  break;
                //case AceType.SYSTEM_AUDIT_OBJECT_ACE_TYPE:
                //  ace = AceType4.read(buffer, offset, startPos);
                //  break;
                //case AceType.SYSTEM_RESOURCE_ATTRIBUTE_ACE_TYPE:
                //ace = AceType3.read(buffer, offset, startPos);
                //break;
                default:
                  throw new NotImplementedException();
            }
            return ace;
          }
      }
}
