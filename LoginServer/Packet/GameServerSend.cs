using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace LoginServer.Packet
{
    class GameServerSend
    {
        public enum SEND_HEADER
        {
            Init = 0x01,
            User_Login = 0x02,
            Chat = 0x99
        }
        private const byte SERVER_EXTENDED_PACKET_TYPE = 0xFF;

        public static void Send(Connection pCon, Packet.SendPacketHandlers.Packet p)
        {
            int packetLength = 0;
            byte[] sendBuffer;
            sendBuffer = p.Compile(pCon.client.PrivateKey, pCon.client.SendKeyOffset, out packetLength);
            pCon.client.SendKeyOffset++;
            if (pCon.client.SendKeyOffset >= pCon.client.PrivateKey.Length) pCon.client.SendKeyOffset = 0;
            if (Program.DEBUG_send)
            {
                string text;
                text = String.Format("GameServerSend::SEND Send packet type: 0x{0:x2} Length: {1} and after crypt: {2}", p.PacketType, p.PacketLength, packetLength);
                Output.WriteLine(text);
            }
            //sendQueue.Enqueue(sendBuffer);
            //connectedSocket.BeginSend(sendBuffer, 0, packetLength, 0, new AsyncCallback(SendCallback), connectedSocket);
            //using blocking send mayby async will be bether??
            try
            {
                int iResult = pCon.SendSocket.AcceptSocket.Send(sendBuffer, 0, packetLength, 0);
                if (iResult == (int)SocketError.SocketError)
                {
                    Output.WriteLine("GameServerSend::Send -  Send failed with error: " + iResult.ToString());
                }
            }
            catch (ObjectDisposedException e)
            {
                Output.WriteLine("GameServerSend::Send -  Send failed with error: " + e.ToString());
            }
        }

        public sealed class Init : Packet.SendPacketHandlers.Packet
        {
            public Init()
                   : base((byte)SERVER_EXTENDED_PACKET_TYPE, 24)
            {
                streamWriter.Write((byte)SEND_HEADER.Init);
                Random rnd = new Random(DateTime.Now.Millisecond);
                for (int i = 0; i < 23; i++)
                {
                    switch (i)
                    {
                        case 3:
                            streamWriter.Write((byte)0xC1);
                            break;
                        case 6:
                            streamWriter.Write((byte)0x12);
                            break;
                        case 9:
                            streamWriter.Write((byte)0x54);
                            break;
                        case 11:
                            streamWriter.Write((byte)0xC1);
                            break;
                        default:
                            streamWriter.Write((byte)rnd.Next(0, 255));
                            break;
                    }
                }

                if (Program.DEBUG_Game_Send) Output.WriteLine("GameServerSend::INIT Send init packet to game server");
            }
        }

        public sealed class User_Login : Packet.SendPacketHandlers.Packet
        {
            public User_Login(int UID, int PID, byte[] guid, byte key)
                : base((byte)SERVER_EXTENDED_PACKET_TYPE, 26)
            {
                streamWriter.Write((byte)SEND_HEADER.User_Login);
                streamWriter.Write(UID);
                streamWriter.Write(PID);
                streamWriter.Write((byte)key);
                streamWriter.Write(guid);
                if (Program.DEBUG_Game_Send) Output.WriteLine("GameServerSend::User_Login Send User_Login packet to game server UID: " + UID.ToString() + " PID: " + PID.ToString());
            }
        }

    }
}
