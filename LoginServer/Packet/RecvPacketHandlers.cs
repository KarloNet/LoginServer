using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Net;

namespace LoginServer.Packet
{
    class RecvPacketHandlers
    {
        //Login steps
        // 01 - establish connection
        // 02 - client sending Init packet crypted using hashClientKey (calculated at run time from MD5 exe file)
        // 03 - after recv init packet send to client new key ( mainKey )
        // 04 - client sending id/pw crypted with mainKey, check it agains login DB
        // 05 - send to client ip/port of game server and temporary ID with used for veryfing by game server that everything is ok
        // 06 - save in temp DB ip/temporary id of this authenticated user (valid for 1 minut or loged in game server)
        public enum RECV_PACKET_SIZE
        {
            CHARACTER_CREATE = 17
        }

        public enum RECV_HEADER
        {
            INIT                = 0x01,
            LOGIN               = 0x02,
            NEW_LOGIN           = 0x03,
            CHARACTER_SELECT    = 0x04,
            CHARACTER_DELETE    = 0x05,
            CHARACTER_CREATE    = 0x06,
            PING                = 0x98,
            GAME_SERVER_EXT     = 0xFF//only for login server  <-> game server use
        }

        private static Dictionary<byte, RecvPacketHandler> packetHandlers;

        static RecvPacketHandlers()
        {
            packetHandlers = new Dictionary<byte, RecvPacketHandler>();
            Register((byte)RECV_HEADER.INIT, Init);
            Register((byte)RECV_HEADER.LOGIN, Login);
            Register((byte)RECV_HEADER.NEW_LOGIN, NewLogin);
            Register((byte)RECV_HEADER.CHARACTER_SELECT, CharacterSelect);
            Register((byte)RECV_HEADER.CHARACTER_DELETE, CharacterDelete);
            Register((byte)RECV_HEADER.CHARACTER_CREATE, CharacterCreate);
            Register((byte)RECV_HEADER.PING, Ping);
            Register((byte)RECV_HEADER.GAME_SERVER_EXT, GameServerExtend);
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
                Output.WriteLine("RecvPacketHandlers::GetHandler Couldn't find a packet handler for packet with ID: " + packetID.ToString());
            }
            return pHandler;
        }

        private static void Init(Connection pConn, byte[] data)
        {
            UInt16 realL = BitConverter.ToUInt16(data, 0);
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("Init packet recv");
            if (pConn.client.Status != Client.STATUS.Connected)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Init - STATUS != CONNECTED, close connection");
                pConn.Close();
                return;
            }
            if (realL != 7424)//to get real length need to swap bytes
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Init - Wrong packet size, close connection");
                pConn.Close();
                return;
            }
            if (data[Program.receivePrefixLength + 0] != 0x01 || data[Program.receivePrefixLength + 4] != 0x00 || data[Program.receivePrefixLength + 9] != 0x54 || data[Program.receivePrefixLength + 14] != 0x01)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Init - Wrong packet data, close connection");
                pConn.Close();
                return;
            }
            int sKeyTmp = Program.rnd.Next(pConn.client.PrivateKey.Length);// random send key
            if (sKeyTmp >= pConn.client.PrivateKey.Length) sKeyTmp = 0;
            int rKeyTmp = Program.rnd.Next(pConn.client.PrivateKey.Length);//random recv key
            if (rKeyTmp >= pConn.client.PrivateKey.Length) rKeyTmp = 0;
            pConn.Send(new SendPacketHandlers.SendKey1(Program.mainKey, sKeyTmp, rKeyTmp));
            pConn.client.SendKeyOffset = sKeyTmp;
            pConn.client.RecvKeyOffset = rKeyTmp;
            pConn.client.PrivateKey = Program.mainKey;
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Init KEY: " + LoginServer.ByteArrayToHex(pConn.client.PrivateKey) + " SEND KEY OFFSET: " + sKeyTmp.ToString() + " RECV KEY OFFSET: " + rKeyTmp.ToString());
            pConn.client.Status = Client.STATUS.Login;
            return;
        }

        public static void Login(Connection pConn, byte[] data)
        {
            byte[] tmpPW;
            byte[] tmpID;
            byte[] cryptID;
            byte[] cryptPW;
            byte idLength;
            byte pwLength;
            int userID = -1;
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login - Error recv login beafore Init, close connection");
                pConn.Close();
                return;
            }
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Recv login packet");
            pwLength = data[Program.receivePrefixLength];
            if (pwLength < 3) //if (pwLength != 8)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Login fail");
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                pConn.client.NumberOfLoginTrys++;
                if (pConn.client.NumberOfLoginTrys > 3)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Too many fail trys, close connection");
                    pConn.Close();
                }
                return;
            }
            tmpPW = new byte[pwLength];
            Array.Copy(data, Program.receivePrefixLength + 1, tmpPW, 0, pwLength);
            idLength = data[Program.receivePrefixLength + pwLength + 1];
            if (idLength < 3 || idLength > 8)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Login fail [1]");
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                pConn.client.NumberOfLoginTrys++;
                if (pConn.client.NumberOfLoginTrys > 3)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Too many fail trys, close connection");
                    pConn.Close();
                }
                return;
            }
            tmpID = new byte[idLength];
            Array.Copy(data, Program.receivePrefixLength + pwLength + 2, tmpID, 0, idLength);
            cryptID = Crypt.Aes.AES_Encrypt(tmpID, Encoding.ASCII.GetBytes(Program.aesKey), Encoding.ASCII.GetBytes(Program.aesSalt));
            cryptPW = Crypt.Aes.AES_Encrypt(tmpPW, Encoding.ASCII.GetBytes(Program.aesKey), Encoding.ASCII.GetBytes(Program.aesSalt));
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Recv Login: " + Encoding.ASCII.GetString(tmpID) + " PW: " + Encoding.ASCII.GetString(tmpPW));
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Recv Crypted Login: " + LoginServer.ByteArrayToHex(cryptID) + " PW: " + LoginServer.ByteArrayToHex(cryptPW));
            userID = Database.DB_Acces.Login(cryptID, cryptPW);
            if (userID < 0)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Login fail [2] Error: " + userID.ToString());
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.WRONGID));
                pConn.client.NumberOfLoginTrys++;
                if (pConn.client.NumberOfLoginTrys > 3)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Too many fail trys, close connection");
                    pConn.Close();
                }
            }
            else
            {// LOGIN IS OK
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Login is OK  UserID: " + userID.ToString());
                if (Users.Exists(userID))//User currently loged in...
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login User already LOGED IN!");
                    Connection tmpCon;
                    Users.Remove(userID, out tmpCon);
                    if (tmpCon != null)
                    {
                        tmpCon.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.CURRENTLY_LOGGED));
                        tmpCon.Close();
                    }

                }
                if (InGameUsers.Exists(userID))
                {
                    //send to correct GameServer info about user log in -> game server will close corensponding connection
                    //next we should recv from game server info about closed connection and then we remove userID from InGameUsers list
                    //but for now we allow user to continue
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login UserID: " + userID.ToString() + " already IN GAME!");
                }
                pConn.client.UserID = userID;
                if(!Users.Add(userID, pConn))// add this new succesfull login to the loged user list
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Error trying add new user");
                    pConn.Close();
                }
                List<Database.Player> lPlayers = Database.DB_Acces.PlayerList(userID);
                pConn.client.AddPlayer(lPlayers);
                pConn.Send(new SendPacketHandlers.BeginPlayerList(userID));
                foreach (Database.Player row in lPlayers)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Found in DB player character name: " + row.PlayerName);
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter streamWriter = new BinaryWriter(stream);
                    streamWriter.Write((int)row.PlayerPID);
                    streamWriter.Write((string)row.PlayerName);
                    streamWriter.Write((int)row.Strength);
                    streamWriter.Write((int)row.Health);
                    streamWriter.Write((int)row.Intel);
                    streamWriter.Write((int)row.Wisdom);
                    streamWriter.Write((int)row.Agility);
                    streamWriter.Write((int)row.PosX);
                    streamWriter.Write((int)row.PosY);
                    streamWriter.Write((int)row.PosZ);
                    streamWriter.Write((int)row.Race);
                    streamWriter.Write((int)row.Job);
                    streamWriter.Write((int)row.Level);
                    streamWriter.Write((int)row.FaceType);
                    streamWriter.Write((int)row.HairType);
                    streamWriter.Write((int)row.Experience);
                    streamWriter.Write((int)row.ActHealth);
                    streamWriter.Write((int)row.ActMana);
                    streamWriter.Write((int)row.ActRage);
                    streamWriter.Write((int)row.HeadArmor);
                    streamWriter.Write((int)row.GlovesArmor);
                    streamWriter.Write((int)row.ChestArmor);
                    streamWriter.Write((int)row.ShortsArmor);
                    streamWriter.Write((int)row.BootsArmor);
                    streamWriter.Write((int)row.LeftHand);
                    streamWriter.Write((int)row.RightHand);
                    pConn.Send(new SendPacketHandlers.Player(stream.ToArray()));
                }
                pConn.Send(new SendPacketHandlers.EndPlayerList());
                pConn.client.Status = Client.STATUS.CharSelect;
            }
        }

        //Create new user account
        public static void NewLogin(Connection pConn, byte[] data)
        {
            int newID = -1;
            if (pConn.client.Status != Client.STATUS.Login)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin - Error recv NewLogin beafore Init, close connection");
                pConn.Close();
                return;
            }
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin Recv new login packet");
            byte pwLength = data[Program.receivePrefixLength];
            if (pwLength < 3) //if (pwLength != 8)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin NewLogin fail");
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                return;
            }
            byte[] tmpPW = new byte[pwLength];
            Array.Copy(data, Program.receivePrefixLength + 1, tmpPW, 0, pwLength);
            byte idLength = data[Program.receivePrefixLength + pwLength + 1];
            if (idLength < 3 || idLength > 8)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin Login fail [1]");
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                return;
            }
            byte[] tmpID = new byte[idLength];
            Array.Copy(data, Program.receivePrefixLength + pwLength + 2, tmpID, 0, idLength);
            byte[] cryptID = Crypt.Aes.AES_Encrypt(tmpID, Encoding.ASCII.GetBytes(Program.aesKey), Encoding.ASCII.GetBytes(Program.aesSalt));
            byte[] cryptPW = Crypt.Aes.AES_Encrypt(tmpPW, Encoding.ASCII.GetBytes(Program.aesKey), Encoding.ASCII.GetBytes(Program.aesSalt));
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin Recv Login: " + Encoding.ASCII.GetString(tmpID) + " PW: " + Encoding.ASCII.GetString(tmpPW));
            if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin Recv Crypted Login: " + LoginServer.ByteArrayToHex(cryptID) + " PW: " + LoginServer.ByteArrayToHex(cryptPW));
            newID = Database.DB_Acces.NewLogin(cryptID, cryptPW);
            if (newID == -3)
            {
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.UNDEFINED));
                return;
            }
            else if (newID == -2)//login currently exist can't make new one
            {
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin User already exists!");
            }
            else//selected Login succesfull created. say client to reconnect and login with newly created login
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::NewLogin Succesfully inserted new User");
                pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.CONNECT_AGAIN));
                pConn.Close();
            }
        }

        //User select character to log in game
        public static void CharacterSelect(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect Recv select char packet");
            if (pConn.client.Status != Client.STATUS.CharSelect)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect - Error recv CharacterSelect beafore login, close connection");
                pConn.Close();
                return;
            }
            int playerID = BitConverter.ToInt32(data, Program.receivePrefixLength);
            Database.Player p = pConn.client.GetPlayer(playerID);
            if(p == null)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect Selected non existing player!");
                pConn.Close();
                return;
            }else
            {
                //tell correct GameServer about new player login in
                //send to the User data about connection to GameServer
                //close connection with succesful transfered connection to GameServer and save it to the GameClient dictionary ( already connected and in game)
                WorldConnectionListener.GameServer gs = Worlds.GetServer(p.PosX, p.PosY);
                if (gs != null && gs.Connected)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect Selected world: " + gs.Name + " size: [" + gs.StartX.ToString() + "," + gs.StartY.ToString() + "::" + gs.EndX.ToString() + "," + gs.EndY.ToString() + "]");
                    if (InGameUsers.Exists(p.PlayerUID))// erro add this user to inGame list then close connection
                    {
                        pConn.Send(new SendPacketHandlers.LoginError(SendPacketHandlers.LOGIN_ERROR.NOT_ALLOWED));
                        if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect User already exists in game!");
                    }
                    else// send to client info about GameServer and send info to Game server about client
                    {
                        if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterSelect Transfer connection to correct GameServer for selected player name: " + p.PlayerName);
                        byte[] guid = System.Guid.NewGuid().ToByteArray();
                        byte key = (byte)Program.rnd.Next(Program.maxGameServerStartKeyValue);
                        gs.connection.Send(new Packet.GameServerSend.User_Login(pConn.client.UserID, playerID, guid, key));
                        pConn.Send(new Packet.SendPacketHandlers.GameServer(gs.UserIP, gs.UserPort, key, guid, gs.GridSize, gs.TileSizeX, gs.TileSizeY, gs.Xmultiplikator, gs.Ymultiplikator));
                    }
                }
                else
                {
                    Output.WriteLine("RecvPacketHandlers::CharacterSelect Thers no Game Server for position [" + p.PosX.ToString() + "," + p.PosY.ToString() + "]");
                    pConn.Send(new Packet.SendPacketHandlers.GameServerBusy());
                    //pConn.Close();
                }
                //finally close current connection
                pConn.Close();
            }
        }

        //User select character to delete
        public static void CharacterDelete(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterDelete Recv delete char packet");
            if (pConn.client.Status != Client.STATUS.CharSelect)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterDelete - Error recv CharacterDelete beafore login, close connection");
                pConn.Close();
                return;
            }
            int playerID = BitConverter.ToInt32(data, Program.receivePrefixLength);
            Database.Player p = pConn.client.GetPlayer(playerID);
            if(p == null)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterDelete Selected non existing player!");
                pConn.Close();
                return;
            }else
            {
                //delete char from DB
                //send to client confirmation about succes in deletion
                bool fStatus = Database.DB_Acces.DeletePlayer(pConn.client.UserID, playerID);
                if (fStatus)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterDelete player: " + p.PlayerName + " was seccesfully deleted");
                    pConn.client.DeletePlayer(p);
                    pConn.Send(new SendPacketHandlers.OperationStatus((int)SendPacketHandlers.OPERATION_TYPE.CHARACTER_DELETE, (int)SendPacketHandlers.OPERATION_STATUS.SUCCES));
                }
                else
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterDelete player: " + p.PlayerName + " ERROR deleting from DB");
                    pConn.Send(new SendPacketHandlers.OperationStatus((int)SendPacketHandlers.OPERATION_TYPE.CHARACTER_DELETE, (int)SendPacketHandlers.OPERATION_STATUS.FAIL));
                }
            }
        }

        //User create character
        public static void CharacterCreate(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate Recv create char packet");
            if (pConn.client.Status != Client.STATUS.CharSelect)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate - Error recv CharacterCreate beafore login, close connection");
                pConn.Close();
                return;
            }
            if (data.Length < (int)RECV_PACKET_SIZE.CHARACTER_CREATE)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate - packet size too small");
                pConn.Close();//shouldynt happens so close connection ( hacker try to do something? )
                return;
            }
            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);
            ms.Position = Program.receivePrefixLength;//set position in stream to begining of data
            string name = br.ReadString();
            if (name.Length > 10 || name.Length <= 0)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate - Wrong name size");
                pConn.Close();//shouldynt happens so close connection ( hacker try to do something? )
                return;
            }
            br.ReadInt32();//empty one
            int playerRace = br.ReadInt32();
            int playerFace = br.ReadInt32();
            int playerHair = br.ReadInt32();
            bool fStatus = Database.DB_Acces.CreatePlayer(pConn.client.UserID, name, playerRace, playerFace, playerHair);
            //add new character to DB
            //send to client succes packet or fail (if player with selected name already exist ect..)
            if (fStatus)
            {
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate New player added to DB");
                pConn.Send(new SendPacketHandlers.OperationStatus((int)SendPacketHandlers.OPERATION_TYPE.CHARACTER_CREATE, (int)SendPacketHandlers.OPERATION_STATUS.SUCCES));
                //send new list of players belongs now to this user
                List<Database.Player> lPlayers = Database.DB_Acces.PlayerList(pConn.client.UserID);
                pConn.client.ClearPlayerList();
                pConn.client.AddPlayer(lPlayers);
                pConn.Send(new SendPacketHandlers.BeginPlayerList(pConn.client.UserID));
                foreach (Database.Player row in lPlayers)
                {
                    if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Login Found in DB player character name: " + row.PlayerName);
                    MemoryStream stream = new MemoryStream();
                    BinaryWriter streamWriter = new BinaryWriter(stream);
                    streamWriter.Write((int)row.PlayerPID);
                    streamWriter.Write((string)row.PlayerName);
                    streamWriter.Write((int)row.Strength);
                    streamWriter.Write((int)row.Health);
                    streamWriter.Write((int)row.Intel);
                    streamWriter.Write((int)row.Wisdom);
                    streamWriter.Write((int)row.Agility);
                    streamWriter.Write((int)row.PosX);
                    streamWriter.Write((int)row.PosY);
                    streamWriter.Write((int)row.PosZ);
                    streamWriter.Write((int)row.Race);
                    streamWriter.Write((int)row.Job);
                    streamWriter.Write((int)row.Level);
                    streamWriter.Write((int)row.FaceType);
                    streamWriter.Write((int)row.HairType);
                    streamWriter.Write((int)row.Experience);
                    streamWriter.Write((int)row.ActHealth);
                    streamWriter.Write((int)row.ActMana);
                    streamWriter.Write((int)row.ActRage);
                    streamWriter.Write((int)row.HeadArmor);
                    streamWriter.Write((int)row.GlovesArmor);
                    streamWriter.Write((int)row.ChestArmor);
                    streamWriter.Write((int)row.ShortsArmor);
                    streamWriter.Write((int)row.BootsArmor);
                    streamWriter.Write((int)row.LeftHand);
                    streamWriter.Write((int)row.RightHand);
                    pConn.Send(new SendPacketHandlers.Player(stream.ToArray()));
                }
                pConn.Send(new SendPacketHandlers.EndPlayerList());
            }
            else
            {
                //something went wrong
                if (Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::CharacterCreate Error adding new player to DB");
                pConn.Send(new SendPacketHandlers.OperationStatus((int)SendPacketHandlers.OPERATION_TYPE.CHARACTER_CREATE, (int)SendPacketHandlers.OPERATION_STATUS.FAIL));
            }
        }

        //Process packets for GameServer communication
        public static void Ping(Connection pConn, byte[] data)
        {
            if (Program.DEBUG_recv_stage1 || Program.DEBUG_recv) Output.WriteLine("RecvPacketHandlers::Ping " + "Ping");
        }

        //Process packets for GameServer communication
        public static void GameServerExtend(Connection pConn, byte[] data)
        {
            if(data.Length <= 5)
            {
                Output.WriteLine("RecvPacketHandlers::GameServerExtend Wrong packet size");
                pConn.Close();
            }
            Packet.RecvPacketHandler handler = Packet.GameServerRecv.GetHandler(data[5]);
            if (handler != null)
            {
                Packet.OnPacketReceive pHandlerMethod = handler.OnReceive;
                try
                {
                    pHandlerMethod(pConn, data);
                }
                catch (Exception e)
                {
                    Output.WriteLine("RecvPacketHandlers::GameServerExtend - catch exception: " + e.ToString());
                    pConn.Close();
                }
                //set new time for last recved packet
                //LastRecv = DateTime.Now.TimeOfDay;
            }
            else
            {
                Output.WriteLine("Connection::ProcessData " + "Wrong packet - close connection");
                pConn.Close();
            }
        }

    }
}
