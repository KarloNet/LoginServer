using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet
{
    static class Header
    {
        internal static Int32 ProcessPrefix(Connection e, Int32 bytesToProcess)
        {
            Int32 addBitsForDecode = 2;
            Int32 remainingBytesToProcess = bytesToProcess;
            if (e.headerBytesReadCount == 0)
            {
                //if (Program.DEBUG_recv) Console.WriteLine("PrefixHandler :: ProcessPrefix - set buffers for new prefix and message");
                e.header = new Byte[Program.receivePrefixLength + addBitsForDecode];
                //zero message read count becouse we just start to read new message
                e.currMsgBytesRead = 0;
            }
            // If this next if-statement is true, then we have received >=
            // enough bytes to have the prefix. So we can determine the 
            // length of the message that we are working on.
            if (remainingBytesToProcess >= Program.receivePrefixLength + addBitsForDecode - e.headerBytesReadCount)
            {
                //Now copy that many bytes to byteArrayForPrefix. We can use the variable receiveMessageOffset as our main index to show which index to get data from in the TCP buffer.
                Buffer.BlockCopy(e.RecvSocket.Buffer, e.RecvSocket.Offset + e.currentRecvBufferPos, e.header, e.headerBytesReadCount, Program.receivePrefixLength + addBitsForDecode - e.headerBytesReadCount);
                e.currentRecvBufferPos += (Program.receivePrefixLength + addBitsForDecode - e.headerBytesReadCount);
                remainingBytesToProcess -= (Program.receivePrefixLength + addBitsForDecode - e.headerBytesReadCount);
                e.headerBytesReadCount = Program.receivePrefixLength + addBitsForDecode;
                byte[] temp = new byte[e.header.Length];
                Buffer.BlockCopy(e.header, 0, temp, 0, e.header.Length);
                switch (e.client.DecodeType)
                {
                    case Client.DECODE_TYPE.AES:
                        break;
                    case Client.DECODE_TYPE.XOR:
                        e.incMsgLength = Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref temp);
                        break;
                    case Client.DECODE_TYPE.BXOR:
                        break;
                    default://defaul is same like xor
                        {
                            e.incMsgLength = Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref temp);
                            break;
                        }
                }
                if (e.incMsgLength <= 0 || e.incMsgLength > Program.maxMessageLength)//if (e.incMsgLength <= 0 || e.incMsgLength > Program.maxMessageLength)
                {
                    e.headerBytesReadCount = 0;
                    e.currMsgBytesRead = 0;
                    Output.WriteLine("Header::ProcessPrefix - RECV PACKET SIZE INCORRECT : " + e.incMsgLength.ToString());
                    return -1;//return crit packet error  to close this connection!
                }
                else
                {
                    if (e.currMsgBytesRead == 0)
                    {
                        //if (Program.DEBUG_recv) Console.WriteLine("PrefixHandler :: ProcessPrefix - set new buffer for incomming message");
                        e.msg = new Byte[e.incMsgLength];
                        //becouse to decode prefix we need to get first 2 bytes of message we put them back here
                        Buffer.BlockCopy(e.header, 0, e.msg, 0, e.header.Length);
                        //e.currMsgBytesRead += e.currentRecvBufferPos;
                        e.currMsgBytesRead = e.header.Length;
                    }
                }
                byte[] tmpHeader = new byte[1];
                tmpHeader[0] = e.header[4];
                //if (Program.DEBUG_recv) Console.WriteLine("PrefixHandler :: ProcessPrefix - " + "Recv message length = " + e.incMsgLength.ToString() + " Header: " + Program.ByteArrayToHex(tmpHeader));
            }
            //This next else-statement deals with the situation where we have some bytes of this prefix in this receive operation, but not all.
            else
            {
                //Write the bytes to the array where we are putting the prefix data, to save for the next loop.
                Buffer.BlockCopy(e.RecvSocket.Buffer, e.RecvSocket.Offset + e.currentRecvBufferPos, e.header, e.headerBytesReadCount, remainingBytesToProcess);
                e.currentRecvBufferPos += remainingBytesToProcess;
                e.headerBytesReadCount += remainingBytesToProcess;
                remainingBytesToProcess = 0;
            }
            //if (Program.DEBUG_recv) Console.WriteLine("PrefixHandler :: ProcessPrefix - Remaining bytes to read in recv buffer: " + remainingBytesToProcess.ToString());
            return remainingBytesToProcess;
        }
    }
}
