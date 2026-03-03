/* Copyright (C) 2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
namespace SMBLibrary.Server
{
    public class SMBServerOptions
    {
        /// <summary>
        /// DANGEROUS! Can only be safely used with immutable filesystems to allow client-side caching.
        /// When this option is set to <c>true</c> the underlying filesystem must never change.
        /// </summary>
        public bool AlwaysGrantReadOplock { get; set; }
    }
}
