using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.IO;

namespace LoginServer
{
    class LoginServer
    {
        WorldConnectionListener worldListener;
        UserConnectionListener userListener;
        SocketListenerSettings socketSettings;

        byte[] hashClientKey;
        byte[] mainKey;

        //These strings are for output interraction.
        const string info = "/INFO";  //show current server information ( number of connections / threads / pools ect..)
        const string closeString = "/CLOSE";  //shutdown login server
        const string debug = "/DEBUG";  //show debug messages in console
        const string debugUser = "/DEBUG USER";  //show debug messages in console
        const string debugUserRecv = "/DEBUG USER_RECV";  //show debug messages in console
        const string debugUserSend = "/DEBUG USER_SEND";  //show debug messages in console
        const string debugUserRecvStage1 = "/DEBUG RECV";  //show debug messages in console
        const string debugUserSendStage1 = "/DEBUG SEND";  //show debug messages in console
        const string debugGameServer = "/DEBUG GAMESERVER";  //show debug messages in console
        const string debugGameServerRecv = "/DEBUG GAMESERVER_RECV";  //show debug messages in console
        const string debugGameServerSend = "/DEBUG GAMESERVER_SEND";  //show debug messages in console
        const string debugDecrypt = "/DEBUG DECRYPT";  //show debug messages in console
        const string debugEncrypt = "/DEBUG ENCRYPT";  //show debug messages in console
        const string clear = "/CLS";  //clear console window
        const string helpString = "/HELP";  //show commands
        const string infoPlayerInGame = "/INFO PLAYER";  //show commands
        const string reconnectGServers = "/RECONNECT";  //show commands
        //temporary commands?
        const string sendToAll = "/SENDA";    //send packet to all connected clients ( for test only)
        const string testString = "/TEST";   //tests
        const string send = "/SEND"; //send test

        public LoginServer()
        {
            IniFile configServerFile = new IniFile("Config.ini");
            IniFile worldServerFile = new IniFile("Worlds.ini");
            Program.port = configServerFile.GetInteger("INTERNAL", "port", 4444);
            Program.maxNumberOfConnections = configServerFile.GetInteger("INTERNAL", "maxNumberOfConnections", 1000);
            Program.bufferSize = configServerFile.GetInteger("INTERNAL", "bufferSize", 100);
            Program.backlog = configServerFile.GetInteger("INTERNAL", "backlog", 100);
            Program.useWhiteList = configServerFile.GetBoolean("INTERNAL", "whiteList", false);
            Program.useBlackList = configServerFile.GetBoolean("INTERNAL", "blackList", true);
            Program.useTempBlackList = configServerFile.GetBoolean("INTERNAL", "tempBlackList", true);
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, Program.port);
            socketSettings = new SocketListenerSettings(Program.maxNumberOfConnections, Program.backlog, Program.receivePrefixLength, Program.bufferSize, Program.sendPrefixLength, localEndPoint);
            Output.WriteLine(ConsoleColor.Green, "LoginServer config. Port: " + Program.port.ToString() + " Max connections: " + Program.maxNumberOfConnections.ToString() + " Buffer size: " + Program.bufferSize.ToString() );
            worldListener = new WorldConnectionListener();
            userListener = new UserConnectionListener(socketSettings);
        }

        public bool Init()
        {
            MD5 md5 = MD5.Create();
            if (File.Exists("ClientMD5"))
            {
                using (var stream = File.OpenRead("ClientMD5"))
                {
                    md5.ComputeHash(stream);
                }
                hashClientKey = md5.Hash;
            }
            else
            {
                Output.WriteLine( ConsoleColor.Red, "Error read client file, using default hash");
                hashClientKey = new byte[1];
                hashClientKey[0] = 0xAC;
            }
            Output.WriteLine("Computed hash is: " + ByteArrayToHex(hashClientKey));
            //read on program start DO NOT HARD CODE IT!
            mainKey = Encoding.ASCII.GetBytes("xSd#4%25*Be#sI(8L6Hg$f18jGt9-lN5F6H43sRgB&8#dG6J9!fjmN7Yhj#2");//60 symbols this key is used after succesfull init connection with client
            Crypt.Xor.Key = mainKey;
            Program.hashClientKey = hashClientKey;//this key is used only for init connection to make sure that connecting client is ok ( both server and client has it hardcoded
            Program.mainKey = mainKey;
            //read on program start DO NOT HARD CODE IT!
            Program.aesKey = "SDFT57G@57G$%23&$23B^H%6GU0B-GH5S654D76F3^e54y34546w9vqe54YV%";//this key and salt is used for crypt logins in DB its mostly important data. Lost  = cant decode info about logins from DB
            Program.aesSalt = "asdr56HY^dsft*%T";
            //read on program start DO NOT HARD CODE IT!
            Program.dbConnStr = "Data Source = 127.0.0.1\\SQLSERVER; Initial Catalog = TEST_SERVER; User ID = G_TEST; Password = TesT!#AppNew$*";

            if (!worldListener.Init()) return false;
            if (!userListener.Init()) return false;
            return true;
        }

        public void Start()
        {
            try
            {
                worldListener.StartListening();
                userListener.StartListening();
                ManageClosing();
            }
            catch( Exception e)
            {
                Output.WriteLine(ConsoleColor.Red, e.ToString());

            }
            CleanUpOnExit();
        }

        void CleanUpOnExit()
        {
            userListener.CleanUpOnExit();
            worldListener.CleanUpOnExit();
        }

        void ManageClosing()
        {
            string stringToCompare = "";
            string theEntry = "";
            string entry = "";
            string entryData = "";

            while (stringToCompare != closeString)
            {
                entry = Output.ReadLine();
                if (entry.IndexOf(" ") > 0)
                {
                    theEntry = entry.Substring(0, entry.IndexOf(" ")).ToUpper();
                    if (entry.IndexOf(" ") + 1 < entry.Length)
                    {
                        entryData = entry.Substring(entry.IndexOf(" ") + 1).ToUpper();
                    }
                    else
                    {
                        entryData = "";
                    }
                }
                else
                {
                    theEntry = entry.ToUpper();
                    entryData = "";
                }

                switch (theEntry)
                {
                    case send:
                        break;
                    case clear:
                        Output.Clear();
                        break;
                    case debug:
                        switch (entryData)
                        {
                            case "":
                                if (Program.DEBUG_Decrypt || Program.DEBUG_Encrypt || Program.DEBUG_Game_Send || Program.DEBUG_Game_Recv || Program.DEBUG_send || Program.DEBUG_recv)
                                {
                                    Program.DEBUG_recv = false;
                                    Program.DEBUG_send = false;
                                    Program.DEBUG_Game_Recv = false;
                                    Program.DEBUG_Game_Send = false;
                                    Program.DEBUG_Decrypt = false;
                                    Program.DEBUG_Encrypt = false;
                                    Output.WriteLine("DEBUG MOD OFF");
                                }else
                                {
                                    Output.WriteLine("DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                    Program.DEBUG_send = true;
                                    Program.DEBUG_Game_Recv = true;
                                    Program.DEBUG_Game_Send = true;
                                    Program.DEBUG_Decrypt = true;
                                    Program.DEBUG_Encrypt = true;
                                }
                                break;
                            case "USER":
                                if (Program.DEBUG_recv || Program.DEBUG_send)
                                {
                                    Program.DEBUG_recv = false;
                                    Program.DEBUG_send = false;
                                    Output.WriteLine("USER DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                    Program.DEBUG_send = true;
                                }
                                break;
                            case "USER_RECV":
                                if (Program.DEBUG_recv)
                                {
                                    Program.DEBUG_recv = false;
                                    Output.WriteLine("USER RECV DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER RECV DEBUG MOD ON");
                                    Program.DEBUG_recv = true;
                                }
                                break;
                            case "USER_SEND":
                                if (Program.DEBUG_send)
                                {
                                    Program.DEBUG_send = false;
                                    Output.WriteLine("USER SEND DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER SEND DEBUG MOD ON");
                                    Program.DEBUG_send = true;
                                }
                                break;
                            case "RECV":
                                if (Program.DEBUG_recv_stage1)
                                {
                                    Program.DEBUG_recv_stage1 = false;
                                    Output.WriteLine("USER RECV STAGE 1 DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER RECV STAGE 1 DEBUG MOD ON");
                                    Program.DEBUG_recv_stage1 = true;
                                }
                                break;
                            case "SEND":
                                if (Program.DEBUG_send_stage1)
                                {
                                    Program.DEBUG_send_stage1 = false;
                                    Output.WriteLine("USER SEND STAGE 1 DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("USER SEND STAGE 1 DEBUG MOD ON");
                                    Program.DEBUG_send_stage1 = true;
                                }
                                break;
                            case "GAMESERVER":
                                if (Program.DEBUG_Game_Recv || Program.DEBUG_Game_Send)
                                {
                                    Program.DEBUG_Game_Recv = false;
                                    Program.DEBUG_Game_Send = false;
                                    Output.WriteLine("GAME SERVER DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("GAME SERVER DEBUG MOD ON");
                                    Program.DEBUG_Game_Recv = true;
                                    Program.DEBUG_Game_Send = true;
                                }
                                break;
                            case "GAMESERVER_RECV":
                                if (Program.DEBUG_Game_Recv)
                                {
                                    Program.DEBUG_Game_Recv = false;
                                    Output.WriteLine("GAME SERVER RECV DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("GAME SERVER RECV DEBUG MOD ON");
                                    Program.DEBUG_Game_Recv = true;
                                }
                                break;
                            case "GAMESERVER_SEND":
                                if (Program.DEBUG_Game_Send)
                                {
                                    Program.DEBUG_Game_Send = false;
                                    Output.WriteLine("GAME SERVER SEND DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("GAME SERVER SEND DEBUG MOD ON");
                                    Program.DEBUG_Game_Send = true;
                                }
                                break;
                            case "DECRYPT":
                                if (Program.DEBUG_Decrypt)
                                {
                                    Program.DEBUG_Decrypt = false;
                                    Output.WriteLine("DECRYPT DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("DECRYPT DEBUG MOD ON");
                                    Program.DEBUG_Decrypt = true;
                                }
                                break;
                            case "ENCRYPT":
                                if (Program.DEBUG_Encrypt)
                                {
                                    Program.DEBUG_Encrypt = false;
                                    Output.WriteLine("ENCRYPT DEBUG MOD OFF");
                                }
                                else
                                {
                                    Output.WriteLine("ENCRYPT DEBUG MOD ON");
                                    Program.DEBUG_Encrypt = true;
                                }
                                break;
                            default:
                                Output.WriteLine("WRONG IN COMMAND DATA");
                                break;
                        }
                        break;
                    case reconnectGServers:
                        worldListener.StartListening();
                        Output.WriteLine("Reconnect done");
                        break;
                    case info:
                        switch (entryData)
                        {
                            case "":
                                Output.WriteLine("Number of active connections = " + userListener.ConnectionCount.ToString() + " Active in pool: " + userListener.ActiveConnInPool.ToString() + " and Inactive: " + userListener.InactiveConnInPool.ToString());
                                break;
                            case "PLAYER":
                                Output.WriteLine("Number of in game users = " + InGameUsers.UsersCount().ToString());
                                Output.WriteLine("Number of waiting users = " + Users.Count.ToString());
                                break;
                            default:
                                Output.WriteLine("Unrecognized command");
                                break;
                        }
                        break;
                    case closeString:
                        stringToCompare = closeString;
                        break;
                    case testString:
                        Output.SetOut(Output.OutType.Console);//set default output to window console
                        break;
                    case helpString:
                        Output.WriteLine("Commands:");
                        Output.WriteLine("/help - show commands");
                        Output.WriteLine("/close - close server");
                        Output.WriteLine("info - some info about server");
                        Output.WriteLine("/cls - clear output window");
                        Output.WriteLine("/debug - show debug info");
                        break;
                    default:
                        Output.WriteLine("Unrecognized command");
                        break;
                }
            }
        }

        public static string ByteArrayToHex(byte[] bytes)
        {
            char[] c = new char[bytes.Length * 2];
            byte b;
            for (int i = 0; i < bytes.Length; i++)
            {
                b = ((byte)(bytes[i] >> 4));
                c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(bytes[i] & 0xF));
                c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c);
        }

    }
}
