/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;


namespace SMBLibrary.SMB2.Encryption
{
	public class DefaultAesCcmEncryptionProvider : EncryptionProvider
	{
		public DefaultAesCcmEncryptionProvider(byte[] encryptionKey) : base(encryptionKey)
		{
		}

		public override int NonceLength => 11;

		public override byte[] EncryptMessage(byte[] nonce, byte[] message, byte[] associatedData, out byte[] signature)
		{
			if (_disposedValue)
				throw new ObjectDisposedException(nameof(DefaultAesCcmEncryptionProvider));

			return Utilities.AesCcm.Encrypt(m_encryptionKey, nonce, message, associatedData, SMB2TransformHeader.SignatureLength, out signature);
		}
	}
}
