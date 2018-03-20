using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    class Program
    {
        public static bool DEBUG_recv = false;
        public static bool DEBUG_send = false;
        public static bool DEBUG_recv_stage1 = true;
        public static bool DEBUG_send_stage1 = true;
        public static bool DEBUG_Decrypt = false;
        public static bool DEBUG_Encrypt = false;
        public static bool DEBUG_Game_Send = true;
        public static bool DEBUG_Game_Recv = true;
        //port that is used by LoginServer
        public static int port;
        //max number of active connection
        public static int maxNumberOfConnections;
        //buffer size for recv / send connection
        public static int bufferSize;
        //This is the maximum number of asynchronous accept operations that can be 
        //posted simultaneously. This determines the size of the pool of 
        //SocketAsyncEventArgs objects that do accept operations. Note that this
        //is NOT the same as the maximum number of connections.
        public static int maxSimultaneousAcceptOps;
        //The size of the queue of incoming connections for the listen socket.
        public static int backlog;
        //white list enabled
        public static bool useWhiteList;
        //black list enabled
        public static bool useBlackList;
        //temp black list enabled
        public static bool useTempBlackList;

        public const int receivePrefixLength = 5;
        public const int sendPrefixLength = 5;
        public const int sendHeaderLength = 1;

        public static byte[] hashClientKey;
        public static byte[] mainKey;

        // This is used only for crypt user data in DB
        // dont hardcode key/salt, force input on server start so it will stay only in memory.
        // for more protection can be xored in memory too ?
        public static string aesKey;
        public static string aesSalt;

        //DB connection string (load from file ? xD) - main Login DB
        public static string dbConnStr;
        
        public static int maxMessageLength = 1024;// server-client maximum size of message (not packet, in one packet can be x messages or one message can be in x packets)

        //time in seconds after connection will be closed if no action from connected client
        public static int maxWaitTime = 300;//5 minutes
        public static bool noDelayConnection = true;//set to true if we want to close inactive connections

        public static int maxGameServerStartKeyValue = 65;

        public static Random rnd;

        static void Main(string[] args)
        {
            Output.SetOut(Output.OutType.Console);//set default output to window console
            rnd = new Random(DateTime.Now.Millisecond);
            LoginServer lServer = new LoginServer();
            if (!lServer.Init())
            {
                //Init server fail, terminate program
                terminate();
            }
            lServer.Start();

            Output.WriteLine(ConsoleColor.Yellow, "Login Server closed");
            Output.WriteLine(ConsoleColor.Yellow, "Press any key");
            Output.WaitForKeyPress();
        }

        private static void terminate()
        {
            Output.WriteLine(ConsoleColor.Red, "Login Server terminated!");
            Output.WriteLine(ConsoleColor.Red, "Press any key");
            Output.WaitForKeyPress();
        }

    }
}
