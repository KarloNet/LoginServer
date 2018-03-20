using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet
{
    static class Decrypt
    {
        public static byte[] NewData(byte[] key, int keyOffset, byte[] data)
        {
            if (key == null || data == null)
            {
                Output.WriteLine("Decrypt::NewData - key or data is null");
                return data;
            }

            byte[] Out;
            byte[] tmp = new byte[Program.receivePrefixLength];
            byte head;
            UInt16 finalL;
            UInt16 realL;

            data = Crypt.Xor.Decrypt(data, key, keyOffset);

            tmp[0] = data[1];
            tmp[1] = data[4];
            head = data[2];
            tmp[2] = data[3];
            tmp[3] = data[0];

            tmp[4] = head;

            realL = BitConverter.ToUInt16(tmp, 0);
            finalL = BitConverter.ToUInt16(tmp, 2);

            if(Program.DEBUG_Decrypt) Output.WriteLine("Decrypt::NewData KeyOffset: " + keyOffset.ToString() + " Recv real length: " + realL.ToString() + " final length: " + finalL.ToString());

            Out = new byte[realL - Program.receivePrefixLength];
            //Out[0] = tmp[0];
            //Out[1] = tmp[1];
            //Out[2] = tmp[2];
            //Out[3] = tmp[3];
            //Out[4] = tmp[4];

            //Array.Copy(data, 5, Out, 5, data.Length - 5 - (finalL - realL));
            Array.Copy(data, Program.receivePrefixLength, Out, 0, data.Length - Program.receivePrefixLength - (finalL - realL));
            return Out;
        }

        public static UInt16 GetData(byte[] key, ref byte[] data)
        {
            return GetData(key, 0, ref data);
        }

        public static UInt16 GetData(byte[] key, int keyOffset, ref byte[] data)
        {
            if (key == null || data == null || data.Length < 7 || keyOffset < 0 || keyOffset >= key.Length)
            {
                Output.WriteLine("Decrypt::GetData - key or data is null or too short");
                return 0;
            }

            byte[] tmp = new byte[Program.receivePrefixLength];
            byte head;
            UInt16 finalL;
            UInt16 realL;

            data = Crypt.Xor.Decrypt(data, key, keyOffset);

            tmp[0] = data[1];
            tmp[1] = data[4];
            head = data[2];
            tmp[2] = data[3];
            tmp[3] = data[0];

            tmp[4] = head;

            realL = BitConverter.ToUInt16(tmp, 0);
            finalL = BitConverter.ToUInt16(tmp, 2);

            if(Program.DEBUG_Decrypt) Output.WriteLine("Decrypt::NewData KeyOffset: " + keyOffset.ToString() + "  Recv real length: " + realL.ToString() + " final length: " + finalL.ToString());
            return finalL;
        }
    }
}
