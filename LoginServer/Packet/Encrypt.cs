using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet
{
    class Encrypt
    {
        static Random random = new Random(DateTime.Now.Millisecond);

        public static byte[] NewPacket(byte[] data, byte header, byte[] key, int keyOffset)
        {
            if (key == null || data == null)
            {
                Output.WriteLine("Encrypt::NewPacket - key or data is empty return untouched");
                return data;
            }
            UInt16 addLength = (UInt16)(random.Next(20) + 2);
            if ((UInt64)(data.Length + Program.sendHeaderLength + Program.sendPrefixLength + addLength) >= UInt32.MaxValue)
            {
                Output.WriteLine("Encrypt::NewPacket - Packet size too large - can't send");
                return null;
            }
            byte[] Out = new byte[Program.sendPrefixLength + Program.sendHeaderLength + data.Length + addLength];
            UInt16 realLength = (UInt16)(data.Length + Program.sendPrefixLength + Program.sendHeaderLength);
            UInt16 finalLength = (UInt16)(data.Length + Program.sendPrefixLength + Program.sendHeaderLength + addLength);
            byte[] lReal = new byte[2];
            byte[] lFinal = new byte[2];
            lReal = BitConverter.GetBytes(realLength);
            lFinal = BitConverter.GetBytes(finalLength);
            byte head = header;
            Out[0] = lFinal[1];
            Out[1] = lReal[0];
            Out[2] = head;
            Out[3] = lFinal[0];
            Out[4] = lReal[1];
            byte[] tmp = new byte[1];
            for (int i = 0; i < addLength; i++)
            {
                random.NextBytes(tmp);
                Out[realLength + i] = tmp[0];
            }
            data.CopyTo(Out, 5);
            Out = Crypt.Xor.Encrypt(Out, key, keyOffset);
            return Out;
        } 
    }
}
