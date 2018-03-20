using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace LoginServer
{
    class UserConnectionListener
    {
        BufferMenager recvSendBufMenager;
        //A Semaphore has two parameters, the initial number of available slots and the maximum number of slots. We'll make them the same. 
        //This Semaphore is used to keep from going over max connection number.
        Semaphore theMaxConnectionsEnforcer;
        ConnectionsPool inactiveConPool;
        ConcurrentDictionary <int, Connection> activeConPool;
        Socket listenSocket;
        SocketListenerSettings socketListenerSettings;
        AccesPermisions accesPermisions;
        int activeConnectionCount;

        public UserConnectionListener(SocketListenerSettings theSocketListenerSettings)
        {
            this.socketListenerSettings = theSocketListenerSettings;
            //buffer for all connections, double the number of connections cuz recv and send use own buffer block
            recvSendBufMenager = new BufferMenager(socketListenerSettings.MaxConnections * 2, socketListenerSettings.BufferSize);
            // Create connections count enforcer
            theMaxConnectionsEnforcer = new Semaphore(socketListenerSettings.MaxConnections, socketListenerSettings.MaxConnections);
            //pool of connections with set of max active connections
            inactiveConPool = new ConnectionsPool(socketListenerSettings.MaxConnections);
            activeConPool = new ConcurrentDictionary<int, Connection>();
            activeConnectionCount = 0;
        }

        public bool Init()
        {
            // preallocate pool of objects for recv/send operations
            for (int i = 0; i < this.socketListenerSettings.MaxConnections; i++)
            {
                Connection con = CreateNewConnection();
                if (con == null)
                {
                    return false;
                }
                this.inactiveConPool.Push(con);
            }
            // set up acces permisions to the server
            accesPermisions = new AccesPermisions();
            Output.WriteLine("UserConnectionListener::Init done");
            return true;
        }

        internal Connection CreateNewConnection()
        {
            bool assignSucces;
            Connection con = new Connection();
            //accept - SocketAsyncEventArgs.Completed is an event, (the only event) 
            con.AcceptSocket.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            //set self as reference in UserToken
            con.AcceptSocket.UserToken = con;
            //objects that do receive/send operations need a buffer, assign a byte buffer from the buffer block to this particular SocketAsyncEventArg object
            assignSucces = this.recvSendBufMenager.SetBuffer(con.RecvSocket);
            if (!assignSucces)
            {
                Output.WriteLine(ConsoleColor.Red, "UserConnectionListener::CreateNewRecvSend Buffer assign fail!");
                return null;
            }
            con.RecvSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            //for send
            assignSucces = this.recvSendBufMenager.SetBuffer(con.SendSocket);
            if (!assignSucces)
            {
                Output.WriteLine(ConsoleColor.Red, "UserConnectionListener::CreateNewRecvSend Buffer assign fail!");
                return null;
            }
            con.SendSocket.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            return con;
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error. It might be better to put the code
            //in the ProcessAccept method.
            if (e.AcceptSocket.Connected) ProcessAccept(e);
        }

        // This method is called whenever a receive or send operation completes.
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            //Any code that you put in this method will NOT be called if
            //the operation completes synchronously, which will probably happen when
            //there is some kind of socket error.
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    //This exception will occur if you code the Completed event of some
                    //operation to come to this method, by mistake.
                    throw new ArgumentException("UserConnectionListener::IO_Completed " + "The last operation completed on the socket was not a receive or send");
            }
        }

        public void StartListening()
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(this.socketListenerSettings.LocalEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //bind it to the port
            listenSocket.Bind(this.socketListenerSettings.LocalEndPoint);
            // Start the listener with a backlog of however many connections.
            //"backlog" means pending connections. 
            //The backlog number is the number of clients that can wait for a
            //SocketAsyncEventArg object that will do an accept operation.
            //The listening socket keeps the backlog as a queue. The backlog allows 
            //for a certain # of excess clients waiting to be connected.
            //If the backlog is maxed out, then the client will receive an error when
            //trying to connect. Max # for backlog can be limited by the operating system.
            listenSocket.Listen(this.socketListenerSettings.Backlog);
            Output.WriteLine( ConsoleColor.Green ,"********************* Server is listening *************************");
            // Calls the method which will post accepts on the listening socket.            
            StartAccept();
        }

        // Begins an operation to accept a connection request from the client         
        internal void StartAccept()
        {
            Connection connection = null;
            bool willRaiseEvent = false;

            //wait for free accept object in pool
            this.theMaxConnectionsEnforcer.WaitOne();
            //Get a SocketAsyncEventArgs object to accept the connection.                        
            try
            {
                connection = this.inactiveConPool.Pop();
                //set individual ID for this connections (for live time of this connection)
                connection.AssignTokenId();
                Output.WriteLine("UserConnectionListener::StartAccept " + "New accept object popped");
            }
            catch
            {
                Output.WriteLine(ConsoleColor.Red ,"UserConnectionListener::StartAccept&0001 " + "No free accept object in pool");
                //return;
            }
            if(connection == null)
            {
                throw new ArgumentNullException("UserConnectionListener::StartAccept&0002 " + "Connection can't be NULL!");
            }
            //Socket.AcceptAsync begins asynchronous operation to accept the connection.
            try
            {
                willRaiseEvent = listenSocket.AcceptAsync(connection.AcceptSocket);
            }
            catch (ObjectDisposedException e)
            {
                Output.WriteLine("UserConnectionListener::StartAccept&0003 Async listener: object disposed error");
            }
            //Socket.AcceptAsync returns true if the I/O operation is pending, i.e. is 
            //working asynchronously. The SocketAsyncEventArgs.Completed event on the acceptEventArg parameter 
            //will be raised upon completion of accept op.
            //AcceptAsync will call the AcceptEventArg_Completed
            //method when it completes, because when we created this SocketAsyncEventArgs
            //object before putting it in the pool, we set the event handler to do it.
            //AcceptAsync returns false if the I/O operation completed synchronously.            
            //The SocketAsyncEventArgs.Completed event on the acceptEventArg 
            //parameter will NOT be raised when AcceptAsync returns false.
            if (!willRaiseEvent)
            {
                //The code in this if (!willRaiseEvent) statement only runs 
                //when the operation was completed synchronously. It is needed because 
                //when Socket.AcceptAsync returns false, 
                //it does NOT raise the SocketAsyncEventArgs.Completed event.
                //And we need to call ProcessAccept and pass it the SAEA object.
                //This is only when a new connection is being accepted.
                // Probably only relevant in the case of a socket error.
                ProcessAccept(connection.AcceptSocket);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
        {
            bool canConnect = false;
            //relase accept object
            //this.theMaxAcceptConnectionsEnforcer.Release();

            // This is when there was an error with the accept op. That should NOT
            // be happening often. It could indicate that there is a problem with
            // that socket. If there is a problem, then we would have an infinite
            // loop here, if we tried to reuse that same socket.
            if (acceptEventArgs.SocketError != SocketError.Success)
            {
                // Loop back to post another accept op. Notice that we are NOT
                StartAccept();
                Output.WriteLine( ConsoleColor.Red, "UserConnectionListener::ProcessAccept SocketError, accept from: " + acceptEventArgs.RemoteEndPoint.ToString());
                //Let's destroy this socket, since it could be bad.
                HandleBadAccept((Connection)acceptEventArgs.UserToken);
                return;
            }
            if ( acceptEventArgs.AcceptSocket != null && (System.Net.IPEndPoint)acceptEventArgs.AcceptSocket.RemoteEndPoint != null)
            {
                canConnect = accesPermisions.CanConnect((System.Net.IPEndPoint)acceptEventArgs.AcceptSocket.RemoteEndPoint);
            }
            if (!canConnect)
            {
                StartAccept();
                HandleBadAccept((Connection)acceptEventArgs.UserToken);
                return;
            }
            //A new socket was created by the AcceptAsync method. The SocketAsyncEventArgs object which did the accept operation has that 
            //socket info in its AcceptSocket property. Now we will give a reference for that socket to the SocketAsyncEventArgs object which will do receive/send.
            ((Connection)acceptEventArgs.UserToken).PrepareForRecvSend();
            ((Connection)acceptEventArgs.UserToken).ReInit(Program.noDelayConnection, Program.maxWaitTime);//clear all internal states to default values for new connection
            activeConPool.TryAdd(((Connection)acceptEventArgs.UserToken).TokenID, (Connection)acceptEventArgs.UserToken);
            Output.WriteLine(ConsoleColor.Gray, "UserConnectionListener::ProcessAccept " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
            Output.WriteLine( ConsoleColor.Gray,"UserConnectionListener::ProcessAccept Client connected : " + ((Connection)acceptEventArgs.UserToken).ConnectionIP );
            Interlocked.Increment(ref activeConnectionCount);
            StartReceive((Connection)acceptEventArgs.UserToken);
            // if its look like OK connection pass it forward
            StartAccept();
        }

        private void HandleBadAccept(Connection con)
        {
            con.Close();
            //Put the SAEA back in the pool.
            inactiveConPool.Push(con);
            this.theMaxConnectionsEnforcer.Release();
        }

        private void HandleBadConnection(Connection con)
        {
            Connection tmpCon;
            Interlocked.Decrement(ref activeConnectionCount);
            con.Close();
            activeConPool.TryRemove(con.TokenID, out tmpCon);
            //Put the SAEA back in the pool.
            inactiveConPool.Push(con);
            this.theMaxConnectionsEnforcer.Release();
        }

        // Set the receive buffer and post a receive op.
        private void StartReceive(Connection re)
        {
            // Post async receive operation on the socket.
            bool willRaiseEvent = false;
            try
            {
                willRaiseEvent = re.RecvSocket.AcceptSocket.ReceiveAsync(re.RecvSocket);
            }
            catch (ObjectDisposedException)
            {
                HandleBadConnection(re);
                willRaiseEvent = true;
            }
            catch (NullReferenceException)
            {
                HandleBadConnection(re);
                willRaiseEvent = true;
            }
            if (!willRaiseEvent)
            {
                //If the op completed synchronously, we need to call ProcessReceive method directly.
                ProcessReceive(re.RecvSocket);
            }
        }

        // This method is invoked by the IO_Completed method when an asynchronous receive operation completes. 
        private void ProcessReceive(SocketAsyncEventArgs re)
        {
            // If there was a socket error, close the connection. This is NOT a normal situation, if you get an error here.
            if (re.SocketError != SocketError.Success)
            {
                Output.WriteLine(ConsoleColor.Red, "UserConnectionListener::ProcessReceive " + "Receive ERROR");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            // If no data was received, close the connection. This is a NORMAL situation that shows when the client has finished sending data = closed connection
            if (re.BytesTransferred == 0)
            {
                Output.WriteLine("UserConnectionListener::ProcessReceive " + "Receive NO DATA");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            //The BytesTransferred property tells us how many bytes we need to process.
            Int32 remainingBytesToProcess = re.BytesTransferred;
            if(Program.DEBUG_recv) Output.WriteLine("RECV packet length: " + remainingBytesToProcess.ToString());
            int status = ((Connection)re.UserToken).Recv(remainingBytesToProcess);
            if (status == -1)// ther was criticall error in recv packet
            {
                Output.WriteLine("UserConnectionListener::ProcessReceive Error in packet processing - close connection");
                HandleBadConnection((Connection)re.UserToken);
                return;
            }
            //wait for next packet
            StartReceive((Connection)re.UserToken);
        }

        // This method is called by I/O Completed() when an asynchronous send completes.  
        private void ProcessSend(SocketAsyncEventArgs sendEventArgs)
        {
            if (sendEventArgs.SocketError == SocketError.Success)
            {
                // If some of the bytes in the message have NOT been sent,
                // then we will need to post another send operation, after we store
                // a count of how many bytes that we sent in this send op.                    
                //receiveSendToken.bytesSentAlreadyCount += receiveSendEventArgs.BytesTransferred;
                // So let's loop back to StartSend().
                //StartSend(receiveSendEventArgs);
                //}
            }
            else
            {
                //If we are in this else-statement, there was a socket error.
                Output.WriteLine(ConsoleColor.Red, "UserConnectionListener::ProcessSend " + "ProcessSend ERROR");
                // We'll just close the socket if there was a socket error when receiving data from the client.
                HandleBadConnection((Connection)sendEventArgs.UserToken);
            }
        }

        internal void CleanUpOnExit()
        {
            DisposeAllSaeaObjects();
        }

        private void DisposeAllSaeaObjects()
        {
            //stop listening port
            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
                listenSocket.Close();
            }
            listenSocket.Dispose();
            inactiveConPool.Iterate(DisposeConnection);
            foreach (var con in activeConPool)
            {
                con.Value.Close();
                con.Value.Dispose();
            }
        }

        bool CloseConnection(Connection con)
        {
            con.Close();
            return true;
        }

        bool DisposeConnection(Connection con)
        {
            con.Dispose();
            return true;
        }

        internal int ConnectionCount
        {
            get
            {
                return activeConnectionCount;
            }
        }

        internal int ActiveConnInPool
        {
            get
            {
                return activeConPool.Count;
            }
        }

        internal int InactiveConnInPool
        {
            get
            {
                return inactiveConPool.Count;
            }
        }
    }
}
