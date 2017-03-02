/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Utilities;

namespace SMBLibrary.SMB2
{
    public abstract class SMB2Command
    {
        public SMB2Header Header;

        public SMB2Command(SMB2CommandName commandName)
        {
            Header = new SMB2Header(commandName);
        }

        public SMB2Command(byte[] buffer, int offset)
        {
            Header = new SMB2Header(buffer, offset);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            Header.WriteBytes(buffer, offset);
            WriteCommandBytes(buffer, offset + SMB2Header.Length);
        }

        public abstract void WriteCommandBytes(byte[] buffer, int offset);

        public byte[] GetBytes()
        {
            byte[] buffer = new byte[this.Length];
            WriteBytes(buffer, 0);
            return buffer;
        }

        public SMB2CommandName CommandName
        {
            get
            {
                return Header.Command;
            }
        }

        public int Length
        {
            get
            {
                return SMB2Header.Length + CommandLength;
            }
        }

        public abstract int CommandLength
        {
            get;
        }

        public static SMB2Command ReadRequest(byte[] buffer, int offset)
        {
            SMB2CommandName Command = (SMB2CommandName)LittleEndianConverter.ToUInt16(buffer, offset + 12);
            switch (Command)
            {
                case SMB2CommandName.Negotiate:
                    return new NegotiateRequest(buffer, offset);
                case SMB2CommandName.SessionSetup:
                    return new SessionSetupRequest(buffer, offset);
                case SMB2CommandName.Logoff:
                    return new LogoffRequest(buffer, offset);
                case SMB2CommandName.TreeConnect:
                    return new TreeConnectRequest(buffer, offset);
                case SMB2CommandName.TreeDisconnect:
                    return new TreeDisconnectRequest(buffer, offset);
                case SMB2CommandName.Create:
                    return new CreateRequest(buffer, offset);
                case SMB2CommandName.Close:
                    return new CloseRequest(buffer, offset);
                case SMB2CommandName.Flush:
                    return new FlushRequest(buffer, offset);
                case SMB2CommandName.Read:
                    return new ReadRequest(buffer, offset);
                case SMB2CommandName.Write:
                    return new WriteRequest(buffer, offset);
                case SMB2CommandName.Lock:
                    return new LockRequest(buffer, offset);
                case SMB2CommandName.IOCtl:
                    return new IOCtlRequest(buffer, offset);
                case SMB2CommandName.Cancel:
                    return new CancelRequest(buffer, offset);
                case SMB2CommandName.Echo:
                    return new EchoRequest(buffer, offset);
                case SMB2CommandName.QueryDirectory:
                    return new QueryDirectoryRequest(buffer, offset);
                case SMB2CommandName.ChangeNotify:
                    return new ChangeNotifyRequest(buffer, offset);
                case SMB2CommandName.QueryInfo:
                    return new QueryInfoRequest(buffer, offset);
                case SMB2CommandName.SetInfo:
                    return new SetInfoRequest(buffer, offset);
                default:
                    throw new System.IO.InvalidDataException("Invalid SMB2 command in buffer");
            }
        }

        public static List<SMB2Command> ReadRequestChain(byte[] buffer, int offset)
        {
            List<SMB2Command> result = new List<SMB2Command>();
            SMB2Command command;
            do
            {
                command = ReadRequest(buffer, offset);
                result.Add(command);
                offset += (int)command.Header.NextCommand;
            }
            while (command.Header.NextCommand != 0);
            return result;
        }

        public static byte[] GetCommandChainBytes(List<SMB2Command> commands)
        {
            return GetCommandChainBytes(commands, null);
        }

        /// <param name="sessionKey">
        /// command will be signed using this key if (not null and) SMB2_FLAGS_SIGNED is set.
        /// </param>
        public static byte[] GetCommandChainBytes(List<SMB2Command> commands, byte[] sessionKey)
        {
            int totalLength = 0;
            for (int index = 0; index < commands.Count; index++)
            {
                // Any subsequent SMB2 header MUST be 8-byte aligned
                int length = commands[index].Length;
                if (index < commands.Count - 1)
                {
                    int paddedLength = (int)Math.Ceiling((double)length / 8) * 8;
                    totalLength += paddedLength;
                }
                else
                {
                    totalLength += length;
                }
            }
            byte[] buffer = new byte[totalLength];
            int offset = 0;
            for (int index = 0; index < commands.Count; index++)
            {
                SMB2Command command = commands[index];
                int commandLength = command.Length;
                int paddedLength;
                if (index < commands.Count - 1)
                {
                    paddedLength = (int)Math.Ceiling((double)commandLength / 8) * 8;
                    command.Header.NextCommand = (uint)paddedLength;
                }
                else
                {
                    paddedLength = commandLength;
                }
                command.WriteBytes(buffer, offset);
                if (command.Header.IsSigned && sessionKey != null)
                {
                    // [MS-SMB2] Any padding at the end of the message MUST be used in the hash computation.
                    byte[] signature = new HMACSHA256(sessionKey).ComputeHash(buffer, offset, paddedLength);
                    // [MS-SMB2] The first 16 bytes of the hash MUST be copied into the 16-byte signature field of the SMB2 Header.
                    ByteWriter.WriteBytes(buffer, offset + SMB2Header.SignatureOffset, signature, 16);
                }
                offset += paddedLength;
            }
            return buffer;
        }
    }
}
