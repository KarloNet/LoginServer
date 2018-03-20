using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LoginServer.Packet
{
    class GameServerRecv
    {
        public enum RECV_HEADER
        {
            BEGIN_USER_LIST = 0x01,
            USER_LIST = 0x02,
            END_USER_LIST = 0x03,
            USER_IN_GAME = 0x04,
            USER_OUT_GAME = 0x05,
            SERVER_INFO = 0x06,
            CHAT = 0x99
        }

        private static Dictionary<byte, RecvPacketHandler> packetHandlers;

        static GameServerRecv()
        {
            packetHandlers = new Dictionary<byte, RecvPacketHandler>();
            Register((byte)RECV_HEADER.BEGIN_USER_LIST, BeginUserList);
            Register((byte)RECV_HEADER.END_USER_LIST, EndUserList);
            Register((byte)RECV_HEADER.USER_LIST, UserList);
            Register((byte)RECV_HEADER.USER_IN_GAME, UserInGame);
            Register((byte)RECV_HEADER.USER_OUT_GAME, UserOutGame);
            Register((byte)RECV_HEADER.SERVER_INFO, ServerInfo);
        }

        private static void Register(byte packetID, OnPacketReceive receiveMethod)
        {
            packetHandlers.Add(packetID, new RecvPacketHandler(packetID, receiveMethod));
        }

        public static RecvPacketHandler GetHandler(byte packetID)
        {
            RecvPacketHandler pHandler = null;
            try
            {
                pHandler = packetHandlers[packetID];
            }
            catch (Exception)
            {
                Output.WriteLine("GameServerRecv::GetHandler Couldn't find a packet handler for packet with ID: " + packetID.ToString());
            }
            return pHandler;
        }

        private static void BeginUserList(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::BeginUserList");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::BeginUserList - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
        }
        
        private static void EndUserList(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::EndUserList");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::EndUserList - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
        }

        private static void UserList(Connection pConn, byte[] data)
        {
            byte[] tmp = new byte[2];
            UInt16 realL;
            tmp[0] = data[1];
            tmp[1] = data[4];
            realL = BitConverter.ToUInt16(tmp, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserList");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserList - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
            int offset = Program.receivePrefixLength + 1;//+1 cuz we use first data byte as extended packet type
            while (true)
            {
                if (offset + 4 <= realL)
                {
                    int pUID = BitConverter.ToInt32(data, offset);
                    bool status = InGameUsers.Add(pUID, 0, pConn.TokenID);
                    Output.WriteLine("RECV user in game UID: " + pUID.ToString() + " STATUS: " + status.ToString());
                }
                else
                {
                    break;
                }
                offset = offset + 4;
            }
        }

        private static void UserInGame(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserInGame");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserInGame - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
            int pUID = BitConverter.ToInt32(data, Program.receivePrefixLength + 1);
            bool status = InGameUsers.Add(pUID, 0, pConn.TokenID);
            Output.WriteLine("RECV new user in game UID: " + pUID.ToString() + " STATUS: " + status.ToString());
        }

        private static void UserOutGame(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserOutGame");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::UserOutGame - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
            int pUID = BitConverter.ToInt32(data, Program.receivePrefixLength + 1);
            InGameUsers.GameUser status = InGameUsers.Remove(pUID);
            Output.WriteLine("RECV user OUT game UID: " + pUID.ToString() + " STATUS: " + (status != null ? "SUCCES" : "FAIL"));
        }

        private static void ServerInfo(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::ServerInfo");
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::ServerInfo - STATUS != LOGIN, close connection");
                pConn.Close();
                return;
            }
            uint xStr;// = BitConverter.ToUInt32(data,         Program.receivePrefixLength + 1);
            uint yStr;// = BitConverter.ToUInt32(data,         Program.receivePrefixLength + 1 + 4);
            uint xEnd;// = BitConverter.ToUInt32(data,         Program.receivePrefixLength + 1 + 4 + 4);
            uint yEnd;// = BitConverter.ToUInt32(data,         Program.receivePrefixLength + 1 + 4 + 4 + 4);
            int port;//  = BitConverter.ToInt32(data,          Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4);
            int gridSize;// = BitConverter.ToInt32(data,       Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4 + 4);
            int tileSizeX;// = BitConverter.ToInt32(data,      Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4 + 4 + 4);
            int tileSizeY;// = BitConverter.ToInt32(data,      Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4 + 4 + 4 + 4);
            int multiplikatorX;// = BitConverter.ToInt32(data, Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4);
            int multiplikatorY;// = BitConverter.ToInt32(data, Program.receivePrefixLength + 1 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4);
            string userIP = "";
            int userPort = 0;

            MemoryStream stream = new MemoryStream(data);
            BinaryReader br;
            using (br = new BinaryReader(stream))
            {
                stream.Position = Program.receivePrefixLength + 1;//set strem position to begin of data (beafore is header data)
                xStr = br.ReadUInt32();
                yStr = br.ReadUInt32();
                xEnd = br.ReadUInt32();
                yEnd = br.ReadUInt32();
                port = br.ReadInt32();
                gridSize = br.ReadInt32();
                tileSizeX = br.ReadInt32();
                tileSizeY = br.ReadInt32();
                multiplikatorX = br.ReadInt32();
                multiplikatorY = br.ReadInt32();
                userIP = br.ReadString();
                userPort = br.ReadInt32();
            }

            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::ServerInfo Game Server MIN(" + xStr.ToString() + "," + yStr.ToString() + ") MAX(" + xEnd.ToString() + "," + yEnd.ToString() + ")");
            if (Program.DEBUG_Game_Recv) Output.WriteLine("GameServerRecv::ServerInfo Game Server USER PORT: " + userPort.ToString());

            pConn.gameServer.StartX = xStr;
            pConn.gameServer.StartY = yStr;
            pConn.gameServer.EndX = xEnd;
            pConn.gameServer.EndY = yEnd;
            pConn.gameServer.UserPort = userPort;
            pConn.gameServer.GridSize = gridSize;
            pConn.gameServer.TileSizeX = tileSizeX;
            pConn.gameServer.TileSizeY = tileSizeY;
            pConn.gameServer.Xmultiplikator = multiplikatorX;
            pConn.gameServer.Ymultiplikator = multiplikatorY;
            pConn.gameServer.UserIP = userIP;
        }
    }
}
