 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace LoginServer
{
    class Connection
    {
        //just for assigning an ID so we can watch our objects while testing.
        static int nextTokenId = 0;
        object locker;
        int tokenID;
        TimeSpan lastActiveTime;
        int errorCount;
        SocketAsyncEventArgs sendSocket;
        SocketAsyncEventArgs recvSocket;
        SocketAsyncEventArgs acceptSocket;

        public Client client;
        public WorldConnectionListener.GameServer gameServer;

        public Int32 currentRecvBufferPos;
        public Int32 headerBytesReadCount;
        public Byte[] header;
        public Byte[] msg;
        public Int32 incMsgLength;
        public Int32 currMsgBytesRead;

        private Timer liveTimer;
        bool noDelayConnection;
        int maxWaitTime;

        //Set initial state
        public Connection()
        {
            sendSocket = new SocketAsyncEventArgs();
            recvSocket = new SocketAsyncEventArgs();
            acceptSocket = new SocketAsyncEventArgs();
            client = new Client(Program.hashClientKey);
            locker = new object();
            lastActiveTime = new TimeSpan(0);
            errorCount = 0;
            tokenID = AssignTokenId();
            currentRecvBufferPos = 0;
            //liveTimer = new Timer(new TimerCallback(TimerCallback), stateObject, dueTimeStart, timeInterval);
            noDelayConnection = false;
            maxWaitTime = 300;//default 5 minutes
        }

        private void TimerCallback(object stateObject)
        {
            if (noDelayConnection)
            {
                TimeSpan dif;
                dif = DateTime.Now.TimeOfDay - LastActivTime;
                if (dif.TotalSeconds > maxWaitTime)
                {
                    try
                    {
                        Output.WriteLine("Connection::TimerCallback Close inactive connections with: " + RecvSocket.AcceptSocket.RemoteEndPoint.ToString());
                        Close();
                    }
                    catch (ObjectDisposedException)
                    {
                        liveTimer.Dispose();
                    }
                }
            }
        }
        //maxInactiveTime is in seconds!
        public void ReInit(bool checkForActivConnection, int maxInactiveTime)
        {
            client = new Client(Program.hashClientKey);
            lastActiveTime = new TimeSpan(0);
            errorCount = 0;
            tokenID = AssignTokenId();
            currentRecvBufferPos = 0;
            this.lastActiveTime = DateTime.Now.TimeOfDay;
            this.noDelayConnection = checkForActivConnection;
            this.maxWaitTime = maxInactiveTime;
            if (noDelayConnection)
            {
                //in worst case connection can stay untouched almost as long as maxInactiveTime * 2, so to take this down we call check twice as fast as we realy want
                liveTimer = new Timer(new TimerCallback(TimerCallback), null, maxInactiveTime * 1000, (maxInactiveTime / 2) * 1000);
            }
        }

        public TimeSpan LastActivTime
        {
            get
            {
                TimeSpan tmp;
                lock (this.locker)
                {
                    tmp = this.lastActiveTime;
                }
                return tmp;
            }
            set
            {
                lock (this.locker)
                {
                    this.lastActiveTime = value;
                }
            }
        }

        internal SocketAsyncEventArgs AcceptSocket
        {
            get
            {
                return acceptSocket;
            }
        }

        internal SocketAsyncEventArgs RecvSocket
        {
            get
            {
                return recvSocket;
            }
        }


        internal SocketAsyncEventArgs SendSocket
        {
            get
            {
                return sendSocket;
            }
        }

        public int AssignTokenId()
        {
            tokenID = Interlocked.Increment(ref nextTokenId);
            return tokenID;
        }

        internal int TokenID
        {
            get
            {
                return tokenID;
            }
        }

        internal string ConnectionIP
        {
            get
            {
                return ((System.Net.IPEndPoint)acceptSocket.AcceptSocket.RemoteEndPoint).Address.ToString();
            }
        }

        //set connection ready for recv and send operations ( acceptSocked was succesfully accepted )
        public void PrepareForRecvSend()
        {
            if (acceptSocket.AcceptSocket != null)
            {
                recvSocket.AcceptSocket = acceptSocket.AcceptSocket;
                recvSocket.UserToken = this;
                sendSocket.AcceptSocket = acceptSocket.AcceptSocket;
                sendSocket.UserToken = this;
            }
        }

        //set connection ready for recv and send operations ( acceptSocked was succesfully accepted )
        public void PrepareForRecvSend(object obj)
        {
            if (acceptSocket.AcceptSocket != null)
            {
                recvSocket.AcceptSocket = acceptSocket.AcceptSocket;
                recvSocket.UserToken = obj;
                sendSocket.AcceptSocket = acceptSocket.AcceptSocket;
                sendSocket.UserToken = obj;
            }
        }


        //accept connection
        public void Accept()
        {

        }

        //recv from connection
        public int Recv(Int32 remainingBytesToProcess)
        {
            currentRecvBufferPos = 0;//set initial position in recv buffer to start
            while (remainingBytesToProcess > 0)
            {
                //If we have not got all of the prefix already, then we need to work on it here.                                
                if (headerBytesReadCount < Program.receivePrefixLength)
                {
                    remainingBytesToProcess = Packet.Header.ProcessPrefix(this, remainingBytesToProcess);
                }
                else // we have all header bytes so can start to read message
                {
                    // If we have processed the prefix, we can work on the message now. We'll arrive here when we have received enough bytes to read the first byte after the prefix.
                    remainingBytesToProcess = Packet.Data.ProcessMessage(this, remainingBytesToProcess);
                }
            }
            return remainingBytesToProcess;
        }

        public void ProcessData(byte[] data)
        {
            Packet.RecvPacketHandler handler = Packet.RecvPacketHandlers.GetHandler(data[2]);
            if (handler != null)
            {
                Packet.OnPacketReceive pHandlerMethod = handler.OnReceive;
                try
                {
                    pHandlerMethod(this, data);
                }
                catch (Exception e)
                {
                    Output.WriteLine("Connection::ProcessData - catch exception: " + e.ToString());
                    Close();
                }
                //set new time for last recved packet
                //LastRecv = DateTime.Now.TimeOfDay;
            }
            else
            {
                Output.WriteLine("Connection::ProcessData " + "Wrong packet - close connection");
                Close();
            }
        }

        //send to connection
        public void Send(Packet.SendPacketHandlers.Packet p)
        {
            int packetLength = 0;
            byte[] sendBuffer;
            sendBuffer = p.Compile(client.PrivateKey, client.SendKeyOffset, out packetLength);
            client.SendKeyOffset++;
            if (client.SendKeyOffset >= client.PrivateKey.Length) client.SendKeyOffset = 0;
            if (Program.DEBUG_send)
            {
                string text;
                text = String.Format("Send packet type: 0x{0:x2} Length: {1} and after crypt: {2}", p.PacketType, p.PacketLength, packetLength);
                Output.WriteLine("Connection::Send " + text);
            }
            //sendQueue.Enqueue(sendBuffer);
            //connectedSocket.BeginSend(sendBuffer, 0, packetLength, 0, new AsyncCallback(SendCallback), connectedSocket);
            //using blocking send mayby async will be bether??
            try
            {
                int iResult = sendSocket.AcceptSocket.Send(sendBuffer, 0, packetLength, 0);
                if (iResult == (int)SocketError.SocketError)
                {
                    Output.WriteLine("Connection::Send -  Send failed with error: " + iResult.ToString());
                }
            }
            catch (ObjectDisposedException e)
            {
                Output.WriteLine("Connection::Send -  Send failed with error: " + e.ToString());
            }
        }

        //close connection
        public void Close()
        {
            bool shutDownSucces = false;
            //This method closes the socket and releases all resources, both managed and unmanaged. It internally calls Dispose.     
            if (acceptSocket.AcceptSocket != null && acceptSocket.AcceptSocket.Connected)
            {
                acceptSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                shutDownSucces = true;
                acceptSocket.AcceptSocket.Close();
                acceptSocket.AcceptSocket = null;
            }
            if (recvSocket.AcceptSocket != null)
            {
                if (!shutDownSucces)
                {
                    recvSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                    shutDownSucces = true;
                }
                recvSocket.AcceptSocket.Close();
                recvSocket.AcceptSocket = null;
            }
            if (sendSocket.AcceptSocket != null)
            {
                if (!shutDownSucces)
                {
                    sendSocket.AcceptSocket.Shutdown(SocketShutdown.Both);
                    shutDownSucces = true;
                }
                sendSocket.AcceptSocket.Close();
                sendSocket.AcceptSocket = null;
            }
            acceptSocket.AcceptSocket = null;
            recvSocket.AcceptSocket = null;
            sendSocket.AcceptSocket = null;
            //remove this user from loged in table
            Users.Remove(client.UserID, this.tokenID);
            if (liveTimer != null)
            {
                liveTimer.Dispose();
            }
        }

        public void Dispose()
        {
            if(acceptSocket != null) acceptSocket.Dispose();
            if(recvSocket != null) recvSocket.Dispose();
            if(sendSocket != null) sendSocket.Dispose();
            if(liveTimer != null) liveTimer.Dispose();
        }

    }
}
