using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet
{
    static class Data
    {
        public static Int32 ProcessMessage(Connection e, Int32 remainingBytesToProcess)
        {
            Int32 remainingBytes = remainingBytesToProcess;
            if (remainingBytesToProcess <= 0)
            {
                Output.WriteLine("Data::ProcessMessage - RECV PACKET SIZE ZERO!");
                return -1;// error in msg size close connection
            }
            //Create the array where we'll store the complete message, 
            //if it has not been created on a previous receive op.
            if (e.currMsgBytesRead == 0)
            {
                //if (Program.DEBUG_recv) Console.WriteLine("MessageHandler :: ProcessMessage - set new buffer for message");
                if (e.incMsgLength <= 0)
                {
                    e.headerBytesReadCount = 0;
                    e.currMsgBytesRead = 0;
                    Output.WriteLine("Data::ProcessMessage - RECV PACKET DATA SIZE ZERO!");
                    return -1;// error in msg size close connection
                }
                e.msg = new Byte[e.incMsgLength];
            }

            if (remainingBytesToProcess + e.currMsgBytesRead == e.incMsgLength)
            {
                // If we are inside this if-statement, then we got the end of the message. In other words, the total number of bytes we received for this message matched the 
                // message length value that we got from the prefix.
                Buffer.BlockCopy(e.RecvSocket.Buffer, e.RecvSocket.Offset + e.currentRecvBufferPos, e.msg, e.currMsgBytesRead, remainingBytesToProcess);
                //set the header read count to zero - ready to read next message header
                e.currentRecvBufferPos += remainingBytesToProcess;
                e.headerBytesReadCount = 0;
                e.currMsgBytesRead = 0;
                remainingBytes -= remainingBytesToProcess;
                switch (e.client.DecodeType)
                {
                    case Client.DECODE_TYPE.AES:
                        break;
                    case Client.DECODE_TYPE.XOR:
                        Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref e.msg);
                        break;
                    case Client.DECODE_TYPE.BXOR:
                        break;
                    default://defaul is same like xor
                        {
                            Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref e.msg);
                            break;
                        }
                }
                ProcessPacket(e);//process new recved packet
            }
            else
            {
                //we dont have all message, need to make more reads to get full message
                if (remainingBytesToProcess + e.currMsgBytesRead < e.incMsgLength)
                {
                    Buffer.BlockCopy(e.RecvSocket.Buffer, e.RecvSocket.Offset + e.currentRecvBufferPos, e.msg, e.currMsgBytesRead, remainingBytesToProcess);
                    e.currMsgBytesRead += remainingBytesToProcess;
                    e.currentRecvBufferPos += remainingBytesToProcess;
                    remainingBytes -= remainingBytesToProcess;
                }
                //we got here if we have more bytes in recv buffer then curent message length = we have another message here too xD
                else
                {
                    Buffer.BlockCopy(e.RecvSocket.Buffer, e.RecvSocket.Offset + e.currentRecvBufferPos, e.msg, e.currMsgBytesRead, e.incMsgLength - e.currMsgBytesRead);
                    //set the header read count to zero - ready to read next message header
                    e.currentRecvBufferPos += (e.incMsgLength - e.currMsgBytesRead);
                    remainingBytes -= (e.incMsgLength - e.currMsgBytesRead);
                    e.currMsgBytesRead = 0;//ready for next msg
                    e.headerBytesReadCount = 0;//ready for new header
                    switch (e.client.DecodeType)
                    {
                        case Client.DECODE_TYPE.AES:
                            break;
                        case Client.DECODE_TYPE.XOR:
                            Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref e.msg);
                            break;
                        case Client.DECODE_TYPE.BXOR:
                            break;
                        default://defaul is same like xor
                            {
                                Decrypt.GetData(e.client.PrivateKey, e.client.RecvKeyOffset, ref e.msg);
                                break;
                            }
                    }
                    ProcessPacket(e);//process new recved packet
                }
            }
            return remainingBytes;
        }

        private static void ProcessPacket(Connection e)
        {
            e.ProcessData(e.msg);//process new recved packet
            e.client.RecvKeyOffset++;
            if (e.client.RecvKeyOffset >= e.client.PrivateKey.Length) e.client.RecvKeyOffset = 0;
        }
    }
}
