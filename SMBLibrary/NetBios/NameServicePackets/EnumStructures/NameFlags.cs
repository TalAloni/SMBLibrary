using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace SMBLibrary.NetBios
{
    public enum OwnerNodeType : byte
    {
        BNode = 0x00,
        PNode = 0x01,
        MNode = 0x10,
    }

    public struct NameFlags // ushort
    {
        public const int Length = 2;

        public OwnerNodeType NodeType;
        public bool WorkGroup;

        public ushort Value
        {
            get
            {
                ushort value = (ushort)(((byte)NodeType) << 13);
                if (WorkGroup)
                {
                    value |= 0x8000;
                }
                return value;
            }
        }
    }
}
