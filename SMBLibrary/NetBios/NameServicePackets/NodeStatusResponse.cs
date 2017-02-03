/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utilities;

namespace SMBLibrary.NetBios
{
    /// <summary>
    /// [RFC 1002] 4.2.18. NODE STATUS RESPONSE
    /// </summary>
    public class NodeStatusResponse
    {
        public NameServicePacketHeader Header;
        public ResourceRecord Resource;
        // byte NumberOfNames;
        public KeyValuePairList<string, NameFlags> Names = new KeyValuePairList<string, NameFlags>();
        public NodeStatistics Statistics;

        public NodeStatusResponse()
        {
            Header = new NameServicePacketHeader();
            Header.OpCode = NameServiceOperation.QueryResponse;
            Header.Flags = OperationFlags.AuthoritativeAnswer | OperationFlags.RecursionAvailable;
            Header.ANCount = 1;
            Resource = new ResourceRecord();
            Resource.Type = NameRecordType.NBStat;
            Statistics = new NodeStatistics();
        }

        public byte[] GetBytes()
        {
            Resource.Data = GetData();

            MemoryStream stream = new MemoryStream();
            Header.WriteBytes(stream);
            Resource.WriteBytes(stream);
            return stream.ToArray();
        }

        public byte[] GetData()
        {
            MemoryStream stream = new MemoryStream();
            stream.WriteByte((byte)Names.Count);
            foreach (KeyValuePair<string, NameFlags> entry in Names)
            {
                ByteWriter.WriteAnsiString(stream, entry.Key);
                BigEndianWriter.WriteUInt16(stream, (ushort)entry.Value);
            }

            ByteWriter.WriteBytes(stream, Statistics.GetBytes());

            return stream.ToArray();
        }
    }
}
