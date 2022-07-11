/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;

namespace SMBLibrary.SMB2.Encryption
{
	public class DefaultAesCcmDecryptionProvider : DecryptionProvider
	{
		public DefaultAesCcmDecryptionProvider(byte[] decryptionKey) : base(decryptionKey)
		{
		}

		public override int NonceLength => 11;

		public override byte[] DecryptAndAuthenticate(byte[] nonce, byte[] encryptedData, byte[] associatedData, byte[] signature)
		{
			if (m_disposedValue)
				throw new ObjectDisposedException(nameof(DefaultAesCcmDecryptionProvider));

			return Utilities.AesCcm.DecryptAndAuthenticate(m_decryptionKey, nonce, encryptedData, associatedData, signature);
		}
	}
}
