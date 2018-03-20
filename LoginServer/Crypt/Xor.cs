using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Crypt
{
    static class Xor
    {
        private static byte keyElementLenght = 1;
        private static byte[] key;


        public static byte[] Encrypt(byte[] toEncrypt)
        {
            if (key.Length <= 0 || toEncrypt.Length <= 0) return new byte[0];

            return Encrypt(toEncrypt, key, 0);
        }

        public static byte[] Encrypt(byte[] toEncrypt, byte[] key, int keyOffset)
        {
            if (key.Length <= 0 || toEncrypt.Length <= 0) return new byte[0];

            byte[] Out = new byte[toEncrypt.Length];
            //simple xor with key
            for (int i = 0; i < toEncrypt.Length; i++)
            {
                //Out[i] = (byte)(toEncrypt[i] ^ key[i % (key.Length / keyElementLenght)]);
                Out[i] = (byte)(toEncrypt[i] ^ key[keyOffset]);
            }
            //now xor with one char beafore
            for (int i = toEncrypt.Length - 2; i > 0; i--)
                Out[i] = (byte)(Out[i] ^ Out[i + 1]);
            if (Program.DEBUG_Encrypt)
            {
                string tmp = LoginServer.ByteArrayToHex(Out);
                Output.WriteLine("ENCODE WITH KEY: " + keyOffset.ToString() + " ENCODED: " + tmp);
            }
            return Out;
        }

        public static byte[] Decrypt(byte[] toDecrypt)
        {
            if (key.Length <= 0 || toDecrypt.Length <= 0) return new byte[0];
            return Decrypt(toDecrypt, key, 0, 0);
        }

        public static byte[] Decrypt(byte[] toDecrypt, byte[] key, int keyOffset)
        {
            if (key.Length <= 0 || toDecrypt.Length <= 0) return new byte[0];
            return Decrypt(toDecrypt, key, 0, keyOffset);
        }

        public static byte[] Decrypt(byte[] toDecrypt, byte[] key, int startOffset, int keyOffset)
        {
            if (key.Length <= 0 || toDecrypt.Length <= 0) return new byte[0];

            byte[] Out = new byte[toDecrypt.Length];
            //xor with one char after
            for (int i = 1 + startOffset; i < toDecrypt.Length - 2; i++)
                toDecrypt[i] = (byte)(toDecrypt[i] ^ toDecrypt[i + 1]);
            //simple xor with key
            for (int i = startOffset; i < toDecrypt.Length; i++)
                toDecrypt[i] = (byte)(toDecrypt[i] ^ key[keyOffset]);
                //toDecrypt[i] = (byte)(toDecrypt[i] ^ key[i % (key.Length / keyElementLenght)]);
            toDecrypt.CopyTo(Out, 0);
            if (Program.DEBUG_Decrypt)
            {
                Output.WriteLine("DECODE WITH KEY: " + keyOffset.ToString());
            }
            return Out;
        }

        public static byte[] Key
        {
            get
            {
                return key;
            }

            set
            {
                if (value.Length > 0)
                {
                    key = value;
                }
                else
                {
                    key = new byte[1];
                    key[0] = 0x12;
                }
            }
        }
    }
}
