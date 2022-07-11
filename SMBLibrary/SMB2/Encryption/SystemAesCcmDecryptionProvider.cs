/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
#if NETSTANDARD2_1_OR_GREATER

using System.Security.Cryptography;

namespace SMBLibrary.SMB2.Encryption
{
	/// <summary>
	/// Encryption strategy which uses the <see cref="System.Security.Cryptography.AesCcm"/> encryption strategy.
	/// </summary>
	public class SystemAesCcmDecryptionProvider : DecryptionProvider
	{
		private readonly AesCcm _aesCcm;

		public SystemAesCcmDecryptionProvider(byte[] encryptionKey) : base(encryptionKey)
		{
			_aesCcm = new AesCcm(encryptionKey);
		}

		public override int NonceLength => 11;

		public override byte[] DecryptAndAuthenticate(byte[] nonce, byte[] encryptedData, byte[] associatedData, byte[] signature)
		{
			byte[] plainText = new byte[encryptedData.Length];
			_aesCcm.Decrypt(nonce, encryptedData, signature, plainText, associatedData);

			return plainText;
		}

		protected override void Dispose(bool disposing)
		{
			_aesCcm.Dispose();
			base.Dispose(disposing);
		}
	}
}
#endif
