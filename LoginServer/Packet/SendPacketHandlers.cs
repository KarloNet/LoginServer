using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LoginServer.Packet
{
    class SendPacketHandlers
    {
        static Random random = new Random(DateTime.Now.Millisecond);

        public enum LOGIN_ERROR
        {
            UNDEFINED = 0x01,
            WRONGID = 0x02,
            WRONG_PASS = 0x03,
            CONNECT_LATER = 0x04,
            BLOCKED = 0x05,
            ID_EXPIRED = 0x06,
            TOO_YOUNG = 0x07,
            NOT_ALLOWED = 0x08,
            CURRENTLY_LOGGED = 0x09,
            CONNECT_AGAIN = 0x10
        }

        public enum SEND_HEADER
        {
            LOGIN_OK = 0x01,//not in use??
            LOGIN_ERROR = 0x02,
            SEND_KEY = 0x03,
            PLAYER = 0x04,
            END_PLAYER_LIST = 0x05,
            GAME_SERVER = 0x06,
            OPERATION_STATUS = 0x07,
            BEGIN_PLAYER_LIST = 0x08,
            GAME_SERVER_BUSY = 0x09,
            CHAT = 0x99
        }

        public enum OPERATION_TYPE
        {
            CHARACTER_CREATE = 0x01,
            CHARACTER_DELETE = 0x02,
            CHARACTER_SELECT = 0x03,
        }

        public enum OPERATION_STATUS
        {
            SUCCES = 0x01,
            FAIL = 0x02
        }

        public abstract class Packet
        {
            protected MemoryStream memStream;
            protected BinaryWriter streamWriter;
            private ushort packetLength;
            private ushort packetDataLength;
            private byte packetType;
            private bool isCompiled;

            public Packet(byte pType, ushort pLength)
            {
                packetType = pType;
                packetDataLength = (ushort)(pLength);
                packetLength = (ushort)(packetDataLength + Program.sendPrefixLength + Program.sendHeaderLength);
                memStream = new MemoryStream((int)packetDataLength);
                streamWriter = new BinaryWriter(memStream);
            }

            public Packet(byte pType)
            {
                packetType = pType;
            }

            public void SetCapacity(ushort newCapacity)
            {
                packetDataLength = (ushort)(newCapacity);
                packetLength = (ushort)(packetDataLength + Program.sendPrefixLength + Program.sendHeaderLength);
                memStream = new MemoryStream(packetDataLength);
                streamWriter = new BinaryWriter(memStream);
            }

            public byte[] Compile(byte[] key, int keyOffset, out int outLength)
            {
                if (!isCompiled)
                {
                    memStream.Position = 0;
                    streamWriter.Write(Encrypt.NewPacket(memStream.ToArray(), packetType, key, keyOffset));
                    isCompiled = true;
                }
                outLength = (int)memStream.Length;
                return memStream.ToArray();
            }

            public byte PacketType { get { return this.packetType; } }
            public ushort PacketLength { get { return this.packetLength; } }
            public ushort PacketDataLength { get { return this.packetDataLength; } }
        }

        public sealed class LoginOK : Packet
        {
            public LoginOK(byte[] inGameGuid, string serverIP, Int32 serverPort, byte key, Int32 id)
                : base((byte)SEND_HEADER.LOGIN_OK)
            {
                SetCapacity((ushort)(16 + serverIP.Length + 2 + 4 + 1 + 1 + 1 + 4 + 1));
                streamWriter.Write(inGameGuid);//always 16 random bytes
                streamWriter.Write(serverIP);
                streamWriter.Write((byte)0x02);
                streamWriter.Write(serverPort);
                streamWriter.Write((byte)0x02);
                streamWriter.Write(key);
                streamWriter.Write((byte)0x02);
                streamWriter.Write(id);
                streamWriter.Write((byte)0x02);
                if(Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send LOGIN_OK");
            }
        }

        public sealed class LoginError : Packet
        {
            public LoginError(LOGIN_ERROR errorNumber)
                : base((byte)SEND_HEADER.LOGIN_ERROR)
            {
                SetCapacity(10);
                for (int i = 0; i < 10; i++)
                {
                    switch (i)
                    {
                        case 3:
                            streamWriter.Write((byte)errorNumber);
                            break;
                        case 6:
                            streamWriter.Write((byte)errorNumber);
                            break;
                        default:
                            streamWriter.Write((byte)random.Next(0, 255));
                            break;
                    }
                }
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send LOGIN_ERROR");
            }
        }

        public sealed class Player : Packet
        {
            public Player(byte[] data)
                : base((byte)SEND_HEADER.PLAYER)
            {
                SetCapacity((ushort)(data.Length));
                streamWriter.Write(data);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send PLAYER");
            }
        }

        public sealed class EndPlayerList : Packet
        {
            public EndPlayerList()
                : base((byte)SEND_HEADER.END_PLAYER_LIST)
            {
                SetCapacity((ushort)(5));
                streamWriter.Write(random.Next());
                streamWriter.Write(random.Next());
                streamWriter.Write(random.Next());
                streamWriter.Write(random.Next());
                streamWriter.Write(random.Next());
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send END_PLAYER_LIST");
            }
        }

        public sealed class SendKey1 : Packet
        {
            public SendKey1(byte[] pKey, int sendKeyOffset, int recvKeyOffset)
                : base((byte)SEND_HEADER.SEND_KEY)
            {
                SetCapacity((ushort)(pKey.Length + 1));
                streamWriter.Write(pKey);
                streamWriter.Write((int)sendKeyOffset);
                streamWriter.Write((int)recvKeyOffset);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send KEY");
            }
        }

        public sealed class GameServer : Packet
        {
            public GameServer(string serverIP, int port, byte startKey, byte[] guid, int gridSize, int tilesizeX, int tileSizeY, int xMultiplikator, int yMultiplikator)
                : base((byte)SEND_HEADER.GAME_SERVER)
            {
                SetCapacity((ushort)(serverIP.Length + 4 + 1 + guid.Length + 20));
                streamWriter.Write(serverIP);
                streamWriter.Write(port);
                streamWriter.Write((byte)startKey);
                streamWriter.Write(guid);
                streamWriter.Write(gridSize);
                streamWriter.Write(tilesizeX);
                streamWriter.Write(tileSizeY);
                streamWriter.Write(xMultiplikator);
                streamWriter.Write(yMultiplikator);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send GAME_SERVER");
            }
        }

        public sealed class OperationStatus : Packet
        {
            public OperationStatus(int operationType, int operationStatus)
                : base((byte)SEND_HEADER.OPERATION_STATUS, 12)
            {
                streamWriter.Write((int)operationType);
                streamWriter.Write((int)0);
                streamWriter.Write((int)operationStatus);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send OPERATION_STATUS");
            }
        }

        public sealed class BeginPlayerList : Packet
        {
            public BeginPlayerList(int uid)
                : base((byte)SEND_HEADER.BEGIN_PLAYER_LIST, 4)
            {
                streamWriter.Write(uid);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send BEGIN_PLAYER_LIST");
            }
        }

        public sealed class GameServerBusy : Packet
        {
            public GameServerBusy()
                : base((byte)SEND_HEADER.GAME_SERVER_BUSY, 4)
            {
                byte[] tmp = new byte[4];
                random.NextBytes(tmp);
                streamWriter.Write((int)BitConverter.ToInt32(tmp, 0));
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send GAME_SERVER_BUSY");
            }
        }

        public sealed class Chat : Packet
        {
            public Chat(string chatName, string chatMessage)
                : base((byte)SEND_HEADER.CHAT)
            {
                SetCapacity((ushort)(chatMessage.Length + chatName.Length + 2));
                streamWriter.Write(chatName.ToCharArray());
                streamWriter.Write((byte)0x00);
                streamWriter.Write(chatMessage.ToCharArray());
                streamWriter.Write((byte)0x00);
                if (Program.DEBUG_send_stage1 || Program.DEBUG_send) Output.WriteLine("Send CHAT");
            }
        }

    }
}
