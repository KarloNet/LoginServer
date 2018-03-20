using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer.Packet
{
    internal delegate void OnPacketReceive(Connection pConn, byte[] data);
    class RecvPacketHandler
    {
        private byte packetID;
        private OnPacketReceive receiveFunction;

        public RecvPacketHandler(byte packID, OnPacketReceive onReceive)
        {
            packetID = packID;
            receiveFunction = onReceive;
        }

        public byte PacketID
        {
            get
            {
                return packetID;
            }
        }

        public OnPacketReceive OnReceive
        {
            get
            {
                return receiveFunction;
            }
        }
    }
}
