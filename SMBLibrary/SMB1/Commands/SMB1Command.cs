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

namespace SMBLibrary.SMB1
{
    public abstract class SMB1Command
    {
        public byte[] SMBParameters; // SMB_Parameters
        public byte[] SMBData; // SMB_Data

        public SMB1Command()
        {
            SMBParameters = new byte[0];
            SMBData = new byte[0];
        }

        public SMB1Command(byte[] buffer, int offset, bool isUnicode)
        {
            byte wordCount = ByteReader.ReadByte(buffer, ref offset);
            SMBParameters = ByteReader.ReadBytes(buffer, ref offset, wordCount * 2);
            ushort byteCount = LittleEndianReader.ReadUInt16(buffer, ref offset);
            SMBData = ByteReader.ReadBytes(buffer, ref offset, byteCount);
        }

        public abstract CommandName CommandName
        {
            get;
        }

        public virtual byte[] GetBytes(bool isUnicode)
        {
            if (SMBParameters.Length % 2 > 0)
            {
                throw new Exception("SMB_Parameters Length must be a multiple of 2");
            }
            int length = 1 + SMBParameters.Length + 2 + SMBData.Length;
            byte[] buffer = new byte[length];
            byte wordCount = (byte)(SMBParameters.Length / 2);
            if (this is NTCreateAndXResponseExtended)
            {
                // [MS-SMB] Section 2.2.4.9.2 and Note <51>:
                // Windows-based SMB servers send 50 (0x32) words in the extended response
                // although they set the WordCount field to 0x2A
                // wordCount SHOULD be 0x2A
                wordCount = 0x2A;
            }
            ushort byteCount = (ushort)SMBData.Length;

            int offset = 0;
            ByteWriter.WriteByte(buffer, ref offset, wordCount);
            ByteWriter.WriteBytes(buffer, ref offset, SMBParameters);
            LittleEndianWriter.WriteUInt16(buffer, ref offset, byteCount);
            ByteWriter.WriteBytes(buffer, ref offset, SMBData);

            return buffer;
        }

        public static SMB1Command ReadCommand(byte[] buffer, int offset, CommandName commandName, SMB1Header header)
        {
            if ((header.Flags & HeaderFlags.Reply) > 0)
            {
                return ReadCommandResponse(buffer, offset, commandName, header.UnicodeFlag);
            }
            else
            {
                return ReadCommandRequest(buffer, offset, commandName, header.UnicodeFlag);
            }
        }

        public static SMB1Command ReadCommandRequest(byte[] buffer, int offset, CommandName commandName, bool isUnicode)
        {
            switch (commandName)
            {
                case CommandName.SMB_COM_CREATE_DIRECTORY:
                    return new CreateDirectoryRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_DELETE_DIRECTORY:
                    return new DeleteDirectoryRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_CLOSE:
                    return new CloseRequest(buffer, offset);
                case CommandName.SMB_COM_FLUSH:
                    return new FlushRequest(buffer, offset);
                case CommandName.SMB_COM_DELETE:
                    return new DeleteRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_RENAME:
                    return new RenameRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_QUERY_INFORMATION:
                    return new QueryInformationRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_SET_INFORMATION:
                    return new SetInformationRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_READ:
                    return new ReadRequest(buffer, offset);
                case CommandName.SMB_COM_WRITE:
                    return new WriteRequest(buffer, offset);
                case CommandName.SMB_COM_CHECK_DIRECTORY:
                    return new CheckDirectoryRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_WRITE_RAW:
                    return new WriteRawRequest(buffer, offset);
                case CommandName.SMB_COM_SET_INFORMATION2:
                    return new SetInformation2Request(buffer, offset);
                case CommandName.SMB_COM_LOCKING_ANDX:
                    return new LockingAndXRequest(buffer, offset);
                case CommandName.SMB_COM_TRANSACTION:
                    return new TransactionRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_TRANSACTION_SECONDARY:
                    return new TransactionSecondaryRequest(buffer, offset);
                case CommandName.SMB_COM_ECHO:
                    return new EchoRequest(buffer, offset);
                case CommandName.SMB_COM_OPEN_ANDX:
                    return new OpenAndXRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_READ_ANDX:
                    return new ReadAndXRequest(buffer, offset);
                case CommandName.SMB_COM_WRITE_ANDX:
                    return new WriteAndXRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_TRANSACTION2:
                    return new Transaction2Request(buffer, offset, isUnicode);
                case CommandName.SMB_COM_TRANSACTION2_SECONDARY:
                    return new Transaction2SecondaryRequest(buffer, offset);
                case CommandName.SMB_COM_FIND_CLOSE2:
                    return new FindClose2Request(buffer, offset);
                case CommandName.SMB_COM_TREE_DISCONNECT:
                    return new TreeDisconnectRequest(buffer, offset);
                case CommandName.SMB_COM_NEGOTIATE:
                    return new NegotiateRequest(buffer, offset);
                case CommandName.SMB_COM_SESSION_SETUP_ANDX:
                    {
                        byte wordCount = ByteReader.ReadByte(buffer, offset);
                        if (wordCount * 2 == SessionSetupAndXRequest.ParametersLength)
                        {
                            return new SessionSetupAndXRequest(buffer, offset, isUnicode);
                        }
                        else if (wordCount * 2 == SessionSetupAndXRequestExtended.ParametersLength)
                        {
                            return new SessionSetupAndXRequestExtended(buffer, offset, isUnicode);
                        }
                        else
                        {
                            throw new InvalidRequestException();
                        }
                    }
                case CommandName.SMB_COM_LOGOFF_ANDX:
                    return new LogoffAndXRequest(buffer, offset);
                case CommandName.SMB_COM_TREE_CONNECT_ANDX:
                    return new TreeConnectAndXRequest(buffer, offset, isUnicode);
                case CommandName.SMB_COM_NT_TRANSACT:
                    return new NTTransactRequest(buffer, offset);
                case CommandName.SMB_COM_NT_TRANSACT_SECONDARY:
                    return new NTTransactSecondaryRequest(buffer, offset);
                case CommandName.SMB_COM_NT_CREATE_ANDX:
                    return new NTCreateAndXRequest(buffer, offset, isUnicode);
                default:
                    throw new NotImplementedException("SMB Command 0x" + commandName.ToString("X"));
            }
        }

        public static SMB1Command ReadCommandResponse(byte[] buffer, int offset, CommandName commandName, bool isUnicode)
        {
            byte wordCount = ByteReader.ReadByte(buffer, offset);
            switch (commandName)
            {
                case CommandName.SMB_COM_CREATE_DIRECTORY:
                    return new CreateDirectoryResponse(buffer, offset);
                case CommandName.SMB_COM_DELETE_DIRECTORY:
                    return new DeleteDirectoryResponse(buffer, offset);
                case CommandName.SMB_COM_CLOSE:
                    return new CloseResponse(buffer, offset);
                case CommandName.SMB_COM_FLUSH:
                    return new FlushResponse(buffer, offset);
                case CommandName.SMB_COM_DELETE:
                    return new DeleteResponse(buffer, offset);
                case CommandName.SMB_COM_RENAME:
                    return new RenameResponse(buffer, offset);
                case CommandName.SMB_COM_QUERY_INFORMATION:
                    return new QueryInformationResponse(buffer, offset);
                case CommandName.SMB_COM_SET_INFORMATION:
                    return new SetInformationResponse(buffer, offset);
                case CommandName.SMB_COM_READ:
                    return new ReadResponse(buffer, offset);
                case CommandName.SMB_COM_WRITE:
                    return new WriteResponse(buffer, offset);
                case CommandName.SMB_COM_CHECK_DIRECTORY:
                    return new CheckDirectoryResponse(buffer, offset);
                case CommandName.SMB_COM_WRITE_RAW:
                    return new WriteRawInterimResponse(buffer, offset);
                case CommandName.SMB_COM_WRITE_COMPLETE:
                    return new WriteRawFinalResponse(buffer, offset);
                case CommandName.SMB_COM_SET_INFORMATION2:
                    return new SetInformation2Response(buffer, offset);
                case CommandName.SMB_COM_LOCKING_ANDX:
                    return new LockingAndXResponse(buffer, offset);
                case CommandName.SMB_COM_TRANSACTION:
                    {
                        if (wordCount * 2 == TransactionInterimResponse.ParametersLength)
                        {
                            return new TransactionInterimResponse(buffer, offset);
                        }
                        else
                        {
                            return new TransactionResponse(buffer, offset);
                        }
                    }
                case CommandName.SMB_COM_ECHO:
                    return new EchoResponse(buffer, offset);
                case CommandName.SMB_COM_OPEN_ANDX:
                    {
                        if (wordCount * 2 == OpenAndXResponse.ParametersLength)
                        {
                            throw new NotImplementedException();
                        }
                        else if (wordCount * 2 == OpenAndXResponseExtended.ParametersLength)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            throw new InvalidRequestException(); ;
                        }
                    }
                case CommandName.SMB_COM_READ_ANDX:
                    return new ReadAndXResponse(buffer, offset, isUnicode);
                case CommandName.SMB_COM_WRITE_ANDX:
                    return new WriteAndXResponse(buffer, offset);
                case CommandName.SMB_COM_TRANSACTION2:
                    {
                        if (wordCount * 2 == Transaction2InterimResponse.ParametersLength)
                        {
                            return new Transaction2InterimResponse(buffer, offset);
                        }
                        else
                        {
                            return new Transaction2Response(buffer, offset);
                        }
                    }
                case CommandName.SMB_COM_FIND_CLOSE2:
                    return new FindClose2Response(buffer, offset);
                case CommandName.SMB_COM_TREE_DISCONNECT:
                    return new TreeDisconnectResponse(buffer, offset);
                case CommandName.SMB_COM_NEGOTIATE:
                    {
                        if (wordCount * 2 == NegotiateResponse.ParametersLength)
                        {
                            return new NegotiateResponse(buffer, offset, isUnicode);
                        }
                        else if (wordCount * 2 == NegotiateResponseExtended.ParametersLength)
                        {
                            return new NegotiateResponseExtended(buffer, offset);
                        }
                        else
                        {
                            throw new InvalidRequestException();;
                        }
                    }
                case CommandName.SMB_COM_SESSION_SETUP_ANDX:
                    if (wordCount * 2 == SessionSetupAndXResponse.ParametersLength)
                    {
                        return new SessionSetupAndXResponse(buffer, offset, isUnicode);
                    }
                    else if (wordCount * 2 == SessionSetupAndXResponseExtended.ParametersLength)
                    {
                        return new SessionSetupAndXResponseExtended(buffer, offset, isUnicode);
                    }
                    else
                    {
                        throw new InvalidRequestException(); ;
                    }
                case CommandName.SMB_COM_LOGOFF_ANDX:
                    return new LogoffAndXResponse(buffer, offset);
                case CommandName.SMB_COM_TREE_CONNECT_ANDX:
                    return new TreeConnectAndXResponse(buffer, offset, isUnicode);
                case CommandName.SMB_COM_NT_TRANSACT:
                    {
                        if (wordCount * 2 == NTTransactInterimResponse.ParametersLength)
                        {
                            return new NTTransactInterimResponse(buffer, offset);
                        }
                        else
                        {
                            return new NTTransactResponse(buffer, offset);
                        }
                    }
                case CommandName.SMB_COM_NT_CREATE_ANDX:
                    return new NTCreateAndXResponse(buffer, offset);
                default:
                    throw new NotImplementedException("SMB Command 0x" + commandName.ToString("X"));
            }
        }

        public static implicit operator List<SMB1Command>(SMB1Command command)
        {
            List<SMB1Command> result = new List<SMB1Command>();
            result.Add(command);
            return result;
        }
    }
}
