/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace SMBLibrary.Server
{
    internal class OpenFileObject
    {
        private uint m_treeID;
        private string m_shareName;
        private string m_path;
        private object m_handle;
        private DateTime m_openedDT;

        public OpenFileObject(uint treeID, string shareName, string path, object handle)
        {
            m_treeID = treeID;
            m_shareName = shareName;
            m_path = path;
            m_handle = handle;
            m_openedDT = DateTime.Now;
        }

        public uint TreeID
        {
            get
            {
                return m_treeID;
            }
        }

        public string ShareName
        {
            get
            {
                return m_shareName;
            }
        }

        public string Path
        {
            get
            {
                return m_path;
            }
            set
            {
                m_path = value;
            }
        }

        public object Handle
        {
            get
            {
                return m_handle;
            }
        }

        public DateTime OpenedDT
        {
            get
            {
                return m_openedDT;
            }
        }
    }
}
