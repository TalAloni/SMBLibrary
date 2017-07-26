/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace SMBLibrary.Win32
{
    internal class PendingRequestCollection
    {
        private Dictionary<IntPtr, List<PendingRequest>> m_handleToNotifyChangeRequests = new Dictionary<IntPtr, List<PendingRequest>>();

        public void Add(PendingRequest request)
        {
            lock (m_handleToNotifyChangeRequests)
            {
                List<PendingRequest> pendingRequests;
                bool containsKey = m_handleToNotifyChangeRequests.TryGetValue(request.FileHandle, out pendingRequests);
                if (containsKey)
                {
                    pendingRequests.Add(request);
                }
                else
                {
                    pendingRequests = new List<PendingRequest>();
                    pendingRequests.Add(request);
                    m_handleToNotifyChangeRequests.Add(request.FileHandle, pendingRequests);
                }
            }
        }

        public void Remove(IntPtr handle, uint threadID)
        {
            lock (m_handleToNotifyChangeRequests)
            {
                List<PendingRequest> pendingRequests;
                bool containsKey = m_handleToNotifyChangeRequests.TryGetValue(handle, out pendingRequests);
                if (containsKey)
                {
                    for (int index = 0; index < pendingRequests.Count; index++)
                    {
                        if (pendingRequests[index].ThreadID == threadID)
                        {
                            pendingRequests.RemoveAt(index);
                            index--;
                        }
                    }

                    if (pendingRequests.Count == 0)
                    {
                        m_handleToNotifyChangeRequests.Remove(handle);
                    }
                }
            }
        }

        public List<PendingRequest> GetRequestsByHandle(IntPtr handle)
        {
            List<PendingRequest> pendingRequests;
            bool containsKey = m_handleToNotifyChangeRequests.TryGetValue((IntPtr)handle, out pendingRequests);
            if (containsKey)
            {
                return new List<PendingRequest>(pendingRequests);
            }
            return new List<PendingRequest>();
        }
    }
}
