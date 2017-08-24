/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using SMBLibrary.SMB1;

namespace SMBLibrary.Server.SMB1
{
    internal class EchoHelper
    {
        internal static List<SMB1Command> GetEchoResponse(EchoRequest request)
        {
            List<SMB1Command> response = new List<SMB1Command>();
            for (int index = 0; index < request.EchoCount; index++)
            {
                EchoResponse echo = new EchoResponse();
                echo.SequenceNumber = (ushort)index;
                echo.Data = request.Data;
                response.Add(echo);
            }
            return response;
        }
    }
}
