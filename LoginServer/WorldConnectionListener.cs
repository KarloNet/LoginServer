using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;

namespace LoginServer
{
    class WorldConnectionListener
    {

        public class GameServer
        {
            string name;
            string ip;
            string userIP;
            int port = -1;
            int userPort = -1;
            int bufSize;
            uint startX;
            uint startY;
            uint endX;
            uint endY;
            int gridSize;
            int tileSizeX;
            int tileSizeY;
            int multiplikatorX;
            int multiplikatorY;
            string key;
            byte[] recvBuf;
            byte[] sendBuf;
            public Connection connection;

            public GameServer()
            {
                name = "";
                userIP = ip = "";
                userPort = port = 0;
                bufSize = 50;
                connection = new Connection();
                connection.gameServer = this;
            }
            public GameServer(string name, string ip, int port, int userPort, int bufSize)
            {
                this.name = name;
                this.ip = this.userIP = ip;
                this.port = port;
                this.userPort = userPort;
                if (bufSize <= 0) bufSize = 50;
                this.bufSize = bufSize;
                connection = new Connection();
                connection.gameServer = this;
            }

            public void InitBuffers() 
            {
                if (recvBuf == null || recvBuf.Length != BufSize)
                {
                    recvBuf = new byte[bufSize];
                }
                if (sendBuf == null || sendBuf.Length != BufSize)
                {
                    sendBuf = new byte[bufSize];
                }
                connection.SendSocket.SetBuffer(sendBuf, 0, BufSize);
            }

            public void ReInit()
            {
                connection = new Connection();
                connection.SendSocket.SetBuffer(sendBuf, 0, BufSize);
                connection.RecvSocket.SetBuffer(recvBuf, 0, BufSize);
                connection.gameServer = this;
                if (key == "")
                {
                    connection.client.PrivateKey = Program.mainKey;
                }
                else
                {
                    connection.client.PrivateKey = Encoding.ASCII.GetBytes(key);
                }
            }

            public string Name { get { return name; } set { name = value; } }
            public string IP { get { return ip; } set { ip = value; } }
            public int Port { get { return port; } set { port = value; } }
            public int UserPort { get { return userPort; } set { userPort = value; } }
            public int BufSize { get { return bufSize; } set { bufSize = value; } }
            public uint StartX { get { return startX; } set { startX = value; } }
            public uint StartY { get { return startY; } set { startY = value; } }
            public uint EndX { get { return endX; } set { endX = value; } }
            public uint EndY { get { return endY; } set { endY = value; } }
            public string Key { get { return key; } set { key = value; } }
            public bool Connected { get { if (connection.AcceptSocket.AcceptSocket != null) return connection.AcceptSocket.AcceptSocket.Connected; else return false; } }
            public int GridSize { get { return gridSize; } set { gridSize = value; } }
            public int TileSizeX { get { return tileSizeX; } set { tileSizeX = value; } }
            public int TileSizeY { get { return tileSizeY; } set { tileSizeY = value; } }
            public int Xmultiplikator { get { return multiplikatorX; } set { multiplikatorX = value; } }
            public int Ymultiplikator { get { return multiplikatorY; } set { multiplikatorY = value; } }
            public string UserIP { get { return userIP; } set { userIP = value; } }
        }

        IniFile worldListFile;
        List<GameServer> gServerList;
        //Socket listenSocket;

        public WorldConnectionListener()
        {
            gServerList = new List<GameServer>();
            Worlds.SetList(gServerList);
        }

        public bool Init()
        {
            string tmpSection = "";
            int i = 0;
            worldListFile = new IniFile("GameServersList.ini");
            while (true)
            {
                tmpSection = "SERVER_" + i.ToString();
                string Name = worldListFile.GetValue(tmpSection, "name");
                string IP = worldListFile.GetValue(tmpSection, "adress");
                int Port = worldListFile.GetInteger(tmpSection, "port");
                int userPort = worldListFile.GetInteger(tmpSection, "userPort");
                int BufSize = worldListFile.GetInteger(tmpSection, "bufferSize");
                string key = worldListFile.GetValue(tmpSection, "key");
                uint sX = worldListFile.GetUInteger(tmpSection, "startX");
                uint sY = worldListFile.GetUInteger(tmpSection, "startY");
                uint eX = worldListFile.GetUInteger(tmpSection, "endX");
                uint eY = worldListFile.GetUInteger(tmpSection, "endY");
                if (IP == "")
                {
                    break;
                }
                else
                {
                    GameServer g = new GameServer(Name, IP, Port, userPort, BufSize);
                    g.StartX = sX;
                    g.StartY = sY;
                    g.EndX = eX;
                    g.EndY = eY;
                    g.Key = key;
                    if (key == "")
                    {
                        g.connection.client.PrivateKey = Program.mainKey;
                    }
                    else
                    {
                        g.connection.client.PrivateKey = Encoding.ASCII.GetBytes(key);
                    }
                    gServerList.Add(g);
                    i++;
                }
            }
            Output.WriteLine("WorldConnectionListener::INIT Loaded " + gServerList.Count.ToString() + " game server data from ini file");
            Output.WriteLine("WorldConnectionListener::INIT done");
            return true;
        }

        public void StartListening()
        {
            for (int i = gServerList.Count - 1; i >= 0; i--)
            {
                GameServer gs = gServerList[i];
                if (!gs.Connected)//server already not connected then try to connect
                {
                    gs.InitBuffers();
                    gs.ReInit();
                    CreateNewConnection(gs);
                    StartClient(gs.connection.AcceptSocket);
                }
            }
        }

        void CreateNewConnection(GameServer gs)
        {
            //accept - SocketAsyncEventArgs.Completed is an event, (the only event) 
            gs.connection.AcceptSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //set self as reference in UserToken
            gs.connection.AcceptSocket.UserToken = gs;
            //objects that do receive/send operations need a buffer, assign a byte buffer from the buffer block to this particular SocketAsyncEventArg object
            gs.connection.RecvSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //for send
            gs.connection.SendSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
        }

        private void StartClient(SocketAsyncEventArgs connectEventArgs)
        {
            //Cast SocketAsyncEventArgs.UserToken to our state object.
            GameServer theConnectingToken = (GameServer)connectEventArgs.UserToken;
            //SocketAsyncEventArgs object that do connect operations on the client
            //are different from those that do accept operations on the server.
            //On the server the listen socket had EndPoint info. And that info was
            //passed from the listen socket to the SocketAsyncEventArgs object 
            //that did the accept operation.
            //But on the client there is no listen socket. The connect socket 
            //needs the info on the Remote Endpoint.
            IPAddress serverIp = IPAddress.Parse(theConnectingToken.IP);
            int serverPort = Convert.ToInt32(theConnectingToken.Port);
            IPEndPoint serverEndPoint = new IPEndPoint(serverIp, serverPort);
            connectEventArgs.RemoteEndPoint = serverEndPoint;
            connectEventArgs.AcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Post the connect operation on the socket.
            //A local port is assigned by the Windows OS during connect op.            
            bool willRaiseEvent = connectEventArgs.AcceptSocket.ConnectAsync(connectEventArgs);
            if (!willRaiseEvent)
            {
                Output.WriteLine("WorldConnectionListener::StartConnect Method if (!willRaiseEvent), id = " + theConnectingToken.connection.TokenID.ToString());
                ProcessConnect(connectEventArgs);
            }
        }

        // This method is called when an operation is completed on a socket 
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    //Output.WriteLine("WorldConnectionListener::IO_Completed CONNECT");
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    //Output.WriteLine("WorldConnectionListener::IO_Completed RECEIVE");
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    //Output.WriteLine("WorldConnectionListener::IO_Completed SEND");
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Disconnect:
                    Output.WriteLine("WorldConnectionListener::IO_Completed DISCONNECT");
                    ProcessDisconnectAndCloseSocket(e);
                    break;
                default:
                    {
                        GameServer receiveSendToken = (GameServer)e.UserToken;
                        throw new ArgumentException("\r\nError in I/O Completed, id = " + receiveSendToken.connection.TokenID);
                    }
            }
        }

        // Pass the connection info from the connecting object to the object
        // that will do send/receive. And put the connecting object back in the pool.
        private void ProcessConnect(SocketAsyncEventArgs connectEventArgs)
        {
            GameServer theConnectingToken = (GameServer)connectEventArgs.UserToken;
            if (connectEventArgs.SocketError == SocketError.Success)
            {
                theConnectingToken.connection.PrepareForRecvSend(theConnectingToken);
                Output.WriteLine("WorldConnectionListener::ProcessConnect Connect id " + theConnectingToken.connection.TokenID.ToString() + " local endpoint = " + IPAddress.Parse(((IPEndPoint)connectEventArgs.AcceptSocket.LocalEndPoint).Address.ToString()) + ": " + ((IPEndPoint)connectEventArgs.AcceptSocket.LocalEndPoint).Port.ToString() + " NAME: " + theConnectingToken.Name);
                StartReceive(theConnectingToken);
                //send init pacet to game server
                Packet.GameServerSend.Send(theConnectingToken.connection, new Packet.GameServerSend.Init());
                theConnectingToken.connection.client.Status = Client.STATUS.Login;
            }
            //This else statement is when there was a socket error
            else
            {
                ProcessConnectionError(connectEventArgs);
            }
        }

        internal void ProcessConnectionError(SocketAsyncEventArgs connectEventArgs)
        {
            GameServer theConnectingToken = (GameServer)connectEventArgs.UserToken;
            Output.WriteLine("WorldConnectionListener::ProcessConnectionError ID = " + theConnectingToken.connection.TokenID.ToString() + ". ERROR: " + connectEventArgs.SocketError.ToString());
            // If connection was refused by server or timed out or not reachable, then we'll keep this socket.
            // If not, then we'll destroy it.
            //if ((connectEventArgs.SocketError != SocketError.ConnectionRefused) && (connectEventArgs.SocketError != SocketError.TimedOut) && (connectEventArgs.SocketError != SocketError.HostUnreachable))
            //{
            theConnectingToken.connection.Close();
            //InGameUsers.RemoveGameServer(theConnectingToken.connection.TokenID);
            //}
        }

        private void ProcessDisconnectAndCloseSocket(SocketAsyncEventArgs receiveSendEventArgs)
        {
            GameServer receiveSendToken = (GameServer)receiveSendEventArgs.UserToken;
            Output.WriteLine("WorldConnectionListener::ProcessDisconnect(), ID = " + receiveSendToken.connection.TokenID.ToString());
            if (receiveSendEventArgs.SocketError != SocketError.Success)
            {
                Output.WriteLine("WorldConnectionListener::ProcessDisconnect ERROR, id " + receiveSendToken.connection.TokenID.ToString());
            }
            //This method closes the socket and releases all resources, both
            //managed and unmanaged. It internally calls Dispose.
            receiveSendToken.connection.Close();
            InGameUsers.RemoveGameServer(receiveSendToken.connection.TokenID);
        }

        private void ProcessSend(SocketAsyncEventArgs receiveSendEventArgs)
        {
            GameServer receiveSendToken = (GameServer)receiveSendEventArgs.UserToken;
            if (receiveSendEventArgs.SocketError == SocketError.Success)
            {
                if (Program.DEBUG_Game_Send) Output.WriteLine("WorldConnectionListener::ProcessSend Send Success, id " + receiveSendToken.connection.TokenID.ToString());
            }
            else
            {
                //If we are in this else-statement, there was a socket error.
                if(Program.DEBUG_Game_Send) Output.WriteLine("WorldConnectionListener::ProcessSend ERROR, id " + receiveSendToken.connection.TokenID.ToString());
                receiveSendToken.connection.Close();
                InGameUsers.RemoveGameServer(receiveSendToken.connection.TokenID);
            }
        }

        // Set the receive buffer and post a receive op.
        private void StartReceive(GameServer re)
        {
            // Post async receive operation on the socket.
            bool willRaiseEvent = false;
            try
            {
                willRaiseEvent = re.connection.RecvSocket.AcceptSocket.ReceiveAsync(re.connection.RecvSocket);
            }
            catch (ObjectDisposedException)
            {
                HandleBadConnection(re.connection);
                InGameUsers.RemoveGameServer(re.connection.TokenID);
                willRaiseEvent = true;
            }
            //catch (NullReferenceException)
            //{
            //    HandleBadConnection(re.connection);
            //    InGameUsers.RemoveGameServer(re.connection.TokenID);
            //    willRaiseEvent = true;
            //}
            if (!willRaiseEvent)
            {
                //If the op completed synchronously, we need to call ProcessReceive method directly.
                ProcessReceive(re.connection.RecvSocket);
            }
        }

        // This method is invoked by the IO_Completed method when an asynchronous receive operation completes. 
        private void ProcessReceive(SocketAsyncEventArgs re)
        {
            // If there was a socket error, close the connection. This is NOT a normal situation, if you get an error here.
            if (re.SocketError != SocketError.Success)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine(ConsoleColor.Red, "WorldConnectionListener::ProcessReceive " + "Receive ERROR");
                HandleBadConnection(((GameServer)re.UserToken).connection);
                InGameUsers.RemoveGameServer(((GameServer)re.UserToken).connection.TokenID);
                return;
            }
            // If no data was received, close the connection. This is a NORMAL situation that shows when the client has finished sending data = closed connection
            if (re.BytesTransferred == 0)
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("WorldConnectionListener::ProcessReceive " + "Receive NO DATA");
                HandleBadConnection(((GameServer)re.UserToken).connection);
                InGameUsers.RemoveGameServer(((GameServer)re.UserToken).connection.TokenID);
                return;
            }
            //The BytesTransferred property tells us how many bytes we need to process.
            Int32 remainingBytesToProcess = re.BytesTransferred;
            //if (Program.DEBUG_Game_Recv) Output.WriteLine("RECV packet length: " + remainingBytesToProcess.ToString());
            int status = ((GameServer)re.UserToken).connection.Recv(remainingBytesToProcess);
            if (status == -1)// ther was criticall error in recv packet
            {
                if (Program.DEBUG_Game_Recv) Output.WriteLine("WorldConnectionListener::ProcessReceive Error in packet processing - close connection");
                HandleBadConnection(((GameServer)re.UserToken).connection);
                InGameUsers.RemoveGameServer(((GameServer)re.UserToken).connection.TokenID);
                return;
            }
            //wait for next packet
            StartReceive((GameServer)re.UserToken);
        }

        private void HandleBadConnection(Connection con)
        {
            con.Close();
        }

        public void CleanUpOnExit()
        {
            for (int i = gServerList.Count - 1; i >= 0; i--)
            {
                GameServer gs = gServerList[i];
                gs.connection.Close();
                InGameUsers.RemoveGameServer(gs.connection.TokenID);
            }
        }

    }
}
