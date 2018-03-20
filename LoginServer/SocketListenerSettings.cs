using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace LoginServer
{
    class SocketListenerSettings
    {
        // the maximum number of connections the sample is designed to handle simultaneously 
        private Int32 maxConnections;

        // max # of pending connections the listener can hold in queue
        private Int32 backlog;

        // buffer size to use for each socket receive operation
        private Int32 bufferSize;

        // length of message prefix for receive ops
        private Int32 receivePrefixLength;

        // length of message prefix for send ops
        private Int32 sendPrefixLength;

        // Endpoint for the listener.
        private IPEndPoint localEndPoint;

        public SocketListenerSettings(Int32 maxConnections, Int32 backlog, Int32 receivePrefixLength, Int32 bufferSize, Int32 sendPrefixLength, IPEndPoint theLocalEndPoint)
        {
            this.maxConnections = maxConnections;
            this.backlog = backlog;
            this.receivePrefixLength = receivePrefixLength;
            this.bufferSize = bufferSize;
            this.sendPrefixLength = sendPrefixLength;
            this.localEndPoint = theLocalEndPoint;
        }

        public Int32 MaxConnections
        {
            get
            {
                return this.maxConnections;
            }
        }
        public Int32 Backlog
        {
            get
            {
                return this.backlog;
            }
        }

        public Int32 ReceivePrefixLength
        {
            get
            {
                return this.receivePrefixLength;
            }
        }
        public Int32 BufferSize
        {
            get
            {
                return this.bufferSize;
            }
        }
        public Int32 SendPrefixLength
        {
            get
            {
                return this.sendPrefixLength;
            }
        }
        public IPEndPoint LocalEndPoint
        {
            get
            {
                return this.localEndPoint;
            }
        }
    }
}
