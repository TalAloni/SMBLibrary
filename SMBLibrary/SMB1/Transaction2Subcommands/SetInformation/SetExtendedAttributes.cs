/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.SMB1
{
    /// <summary>
    /// SMB_INFO_SET_EAS
    /// </summary>
    public class SetExtendedAttributes : SetInformation
    {
        public FullExtendedAttributeList ExtendedAttributeList;

        public SetExtendedAttributes()
        {
            ExtendedAttributeList = new FullExtendedAttributeList();
        }

        public SetExtendedAttributes(byte[] buffer) : this(buffer, 0)
        {
        }

        public SetExtendedAttributes(byte[] buffer, int offset)
        {
            ExtendedAttributeList = new FullExtendedAttributeList(buffer, offset);
        }

        public override byte[] GetBytes()
        {
            return ExtendedAttributeList.GetBytes();
        }
    }
}
