using System;

namespace SMBLibrary
{
    /// <summary>
    /// Name: AsyncExceptionEventArgs
    /// Description: Event arguments for asynchronous exception events.
    /// Author: Ricardo Sanchez - sanchric-forvia
    /// LogBook:
    ///     2025-09-10: Creation
    /// </summary>
    public class AsyncExceptionEventArgs : EventArgs
    {
        public System.Exception Exception { get; set; }
    }
}