/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.Services
{
    /// <summary>
    /// [MS-WKST]
    /// </summary>
    public class WorkstationService : RemoteService
    {
        private uint m_platformID;
        private string m_computerName;
        private string m_lanGroup;
        private uint m_verMajor;
        private uint m_verMinor;

        public WorkstationService(string computerName, string lanGroup)
        {
            m_platformID = (uint)PlatformName.NT;
            m_computerName = computerName;
            m_lanGroup = lanGroup;
            m_verMajor = 5;
            m_verMinor = 2;
        }

        public override byte[] GetResponseBytes(ushort opNum, byte[] requestBytes)
        {
            switch ((WorkstationServiceOpName)opNum)
            {
                case WorkstationServiceOpName.NetrWkstaGetInfo:
                    NetrWkstaGetInfoRequest request = new NetrWkstaGetInfoRequest(requestBytes);
                    NetrWkstaGetInfoResponse response = GetNetrWkstaGetInfoResponse(request);
                    return response.GetBytes();
                default:
                    throw new NotImplementedException();
            }
        }

        public NetrWkstaGetInfoResponse GetNetrWkstaGetInfoResponse(NetrWkstaGetInfoRequest request)
        {
            NetrWkstaGetInfoResponse response = new NetrWkstaGetInfoResponse();
            if (request.Level == 100)
            {
                WorkstationInfo100 info = new WorkstationInfo100();
                info.PlatformID = m_platformID;
                info.ComputerName.Value = m_computerName;
                info.LanGroup.Value  = m_lanGroup;
                info.VerMajor = m_verMajor;
                info.VerMinor = m_verMinor;
                response.WkstaInfo = new WorkstationInfo(info);
            }
            else if (request.Level == 101)
            {
                WorkstationInfo101 info = new WorkstationInfo101();
                info.PlatformID = m_platformID;
                info.ComputerName.Value = m_computerName;
                info.LanGroup.Value = m_lanGroup;
                info.VerMajor = m_verMajor;
                info.VerMinor = m_verMinor;
                info.LanRoot.Value = m_lanGroup;
                response.WkstaInfo = new WorkstationInfo(info);
            }
            else
            {
                throw new NotImplementedException();
            }
            response.Result = Win32Error.ERROR_SUCCESS;
            return response;
        }

        public override Guid InterfaceGuid
        {
            get
            {
                return new Guid("6BFFD098-A112-3610-9833-46C3F87E345A");
            }
        }

        public override string PipeName
        {
            get
            {
                return "wkssvc";
            }
        }
    }
}
