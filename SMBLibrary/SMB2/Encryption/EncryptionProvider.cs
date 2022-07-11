/* Copyright (C) 2020 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;

namespace SMBLibrary.SMB2.Encryption
{
	public abstract class EncryptionProvider : IDisposable
	{
		protected readonly byte[] m_encryptionKey;
		private readonly Random m_random;
		protected bool _disposedValue;

		public abstract int NonceLength { get; }

		protected EncryptionProvider(byte[] encryptionKey)
		{
			m_encryptionKey = encryptionKey;
			m_random = new Random();
		}

		public abstract byte[] EncryptMessage(byte[] nonce, byte[] message, byte[] assosciatedData, out byte[] signature);

		public byte[] GenerateNonce()
		{
			byte[] nonce = new byte[NonceLength];
			m_random.NextBytes(nonce);
			return nonce;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				_disposedValue = true;
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
		public static EncryptionProvider GetProvider(byte[] sessionKey, SMB2Dialect dialect)
		{
			byte[] encryptionKey = SMB2Cryptography.GenerateClientEncryptionKey(sessionKey, dialect, null);
#if NETSTANDARD2_1_OR_GREATER
			try
			{
				return new SystemAesCcmEncryptionProvider(encryptionKey);
			}

			catch (PlatformNotSupportedException)
			{
				return new DefaultAesCcmEncryptionProvider(encryptionKey);
			}
#else
			return new DefaultAesCcmEncryptionProvider(encryptionKey);
#endif


		}
	}
}
