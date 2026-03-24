/* Copyright (C) 2017-2026 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
namespace System.Security.Cryptography
{
    public class RC4
    {
        public static byte[] Encrypt(byte[] key, byte[] data)
        {
            RC4KeyState state = InitializeStateFromKey(key);
            return Encrypt(state, data);
        }

        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            RC4KeyState state = InitializeStateFromKey(key);
            return Encrypt(state, data);
        }

        public static RC4KeyState InitializeStateFromKey(byte[] key)
        {
            byte[] s = new byte[256];
            for (int index = 0; index < 256; index++)
            {
                s[index] = (byte)index;
            }

            for (int i = 0, j = 0; i < 256; i++)
            {
                j = (j + key[i % key.Length] + s[i]) & 255;

                Swap(s, i, j);
            }

            return new RC4KeyState(s);
        }

        public static byte[] Encrypt(RC4KeyState state, byte[] data)
        {
            byte[] s = state.S;

            byte[] output = new byte[data.Length];
            for (int index = 0; index < data.Length; index++)
            {
                state.I = (state.I + 1) & 255;
                state.J = (state.J + state.S[state.I]) & 255;

                Swap(state.S, state.I, state.J);
                output[index] = (byte)(data[index] ^ s[(s[state.I] + s[state.J]) & 255]);
            }
            return output;
        }

        public static byte[] Decrypt(RC4KeyState state, byte[] data)
        {
            return Encrypt(state, data);
        }

        private static void Swap(byte[] state, int i, int j)
        {
            byte c = state[i];

            state[i] = state[j];
            state[j] = c;
        }
    }

    public class RC4KeyState
    {
        internal byte[] S;
        internal int I;
        internal int J;

        internal RC4KeyState(byte[] s)
        {
            S = s;
        }
    }
}
