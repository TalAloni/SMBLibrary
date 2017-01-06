/* Copyright (C) 2014 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.SMB1
{
    public abstract class FindInformationEntry
    {
        private bool m_returnResumeKeys;

        public FindInformationEntry(bool returnResumeKeys)
        {
            m_returnResumeKeys = returnResumeKeys;
        }

        public abstract void WriteBytes(byte[] buffer, ref int offset, bool isUnicode);
        
        public abstract int GetLength(bool isUnicode);

        public bool ReturnResumeKeys
        {
            get
            {
                return m_returnResumeKeys;
            }
        }

        public static FindInformationEntry ReadEntry(byte[] buffer, ref int offset, FindInformationLevel informationLevel, bool isUnicode, bool returnResumeKeys)
        {
            switch (informationLevel)
            {
                case FindInformationLevel.SMB_INFO_STANDARD:
                    return new FindInfoStandard(buffer, ref offset, isUnicode, returnResumeKeys);
                case FindInformationLevel.SMB_INFO_QUERY_EA_SIZE:
                    return new FindInfoQueryEASize(buffer, ref offset, isUnicode, returnResumeKeys);
                case FindInformationLevel.SMB_INFO_QUERY_EAS_FROM_LIST:
                    return new FindInfoQueryExtendedAttributesFromList(buffer, ref offset, isUnicode, returnResumeKeys);
                case FindInformationLevel.SMB_FIND_FILE_DIRECTORY_INFO:
                    return new FindFileDirectoryInfo(buffer, ref offset, isUnicode);
                case FindInformationLevel.SMB_FIND_FILE_FULL_DIRECTORY_INFO:
                    return new FindFileFullDirectoryInfo(buffer, ref offset, isUnicode);
                case FindInformationLevel.SMB_FIND_FILE_NAMES_INFO:
                    return new FindFileNamesInfo(buffer, ref offset, isUnicode);
                case FindInformationLevel.SMB_FIND_FILE_BOTH_DIRECTORY_INFO:
                    return new FindFileBothDirectoryInfo(buffer, ref offset, isUnicode);
                default:
                    throw new InvalidRequestException();;
            }
        }
    }
}
