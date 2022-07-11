/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;


namespace SMBLibrary.SMB2.Encryption
{
	public abstract class DecryptionProvider : IDisposable
	{
		protected readonly byte[] m_decryptionKey;
		protected bool m_disposedValue;

		protected DecryptionProvider(byte[] decryptionKey)
		{
			m_decryptionKey = decryptionKey;
		}
		public abstract int NonceLength { get; }

		public abstract byte[] DecryptAndAuthenticate(byte[] nonce, byte[] encryptedData, byte[] associatedData, byte[] signature);

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposedValue)
			{
				m_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}


		/// <summary>
		/// Gets the appropriate <see cref="EncryptionProvider"/> for the environment.
		/// </summary>
		/// <param name="sessionKey">The session key</param>
		/// <param name="dialect">The dialect to get the key for.</param>
		/// <returns>An encryption strategy.</returns>
		public static DecryptionProvider GetProvider(byte[] sessionKey, SMB2Dialect dialect)
		{
			byte[] encryptionKey = SMB2Cryptography.GenerateClientDecryptionKey(sessionKey, dialect, null);
#if NETSTANDARD2_1_OR_GREATER
			try
			{
				return new SystemAesCcmDecryptionProvider(encryptionKey);
			}

			catch (PlatformNotSupportedException)
			{
				return new DefaultAesCcmDecryptionProvider(encryptionKey);
			}
#else
			return new DefaultAesCcmDecryptionProvider(encryptionKey);
#endif


		}
	}
}
