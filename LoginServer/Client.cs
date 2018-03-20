using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    class Client
    {
        public enum STATUS
        {
            Connected   = 0x01,
            Login       = 0x03,
            CharSelect  = 0x04
        }

        public enum DECODE_TYPE
        {
            XOR = 1134,//xor with key > 1 and xor one after ( default one )
            AES = 1263,//standard aes with aes key and salt
            BXOR = 1080,//xor using only one byte key
        }

        public enum ENCODE_TYPE
        {
            XOR = 3411,//xor with key > 1 and xor one after ( default one )
            AES = 6312,//standard aes with aes key and salt
            BXOR = 8010,//xor using only one byte key
        }

        byte[] privateKey;
        int recvKeyOffset;
        int sendKeyOffset;
        STATUS status;
        int numberOfLoginTry;
        int userID;
        DECODE_TYPE decodeType;
        ENCODE_TYPE encodeType;
        //List<Database.Player> playerList;
        Dictionary<int, Database.Player> playerList;
        public int RecvKeyOffset { get { return recvKeyOffset; } set { recvKeyOffset = value; if (recvKeyOffset > privateKey.Length || recvKeyOffset < 0) recvKeyOffset = 0; } }
        public int SendKeyOffset { get { return sendKeyOffset; } set { sendKeyOffset = value; if (sendKeyOffset > privateKey.Length || sendKeyOffset < 0) sendKeyOffset = 0; } }
        public DECODE_TYPE DecodeType { get { return decodeType; } set { decodeType = value; } }
        public ENCODE_TYPE EncodeType { get { return encodeType; } set { encodeType = value; } }

        public Client(byte[] privateKey)
        {
            this.privateKey = privateKey;
            this.recvKeyOffset = 0;
            this.sendKeyOffset = 0;
            DecodeType = DECODE_TYPE.XOR;
            EncodeType = ENCODE_TYPE.XOR;
            this.status = STATUS.Connected;
            this.numberOfLoginTry = 0;
            userID = -1;
            playerList = new Dictionary<int, Database.Player>();
        }

        public STATUS Status
        {
            get
            {
                return this.status;
            }
            set
            {
                this.status = value;
            }
        }

        public int NumberOfLoginTrys
        {
            get
            {
                return this.numberOfLoginTry;
            }
            set
            {
                this.numberOfLoginTry = value;
            }
        }

        public byte[] PrivateKey
        {
            get
            {
                return this.privateKey;
            }
            set
            {
                this.privateKey = value;
            }
        }

        public int UserID
        {
            get { return this.userID; }
            set { this.userID = value; }
        }

        public void AddPlayer(List<Database.Player> playerList)
        {
            foreach (Database.Player p in playerList)
            {
                this.playerList.Add(p.PlayerPID, p);
            }
        }

        public void AddPlayer(Database.Player p)
        {
            playerList.Add(p.PlayerPID, p);
        }

        public int PlayerCount()
        {
            return playerList.Count;
        }

        public Database.Player GetPlayer(int playerID)
        {
            Database.Player p = null;
            if(playerList.TryGetValue(playerID, out p))
            {
                return p;
            }else
            {
                return null;
            }
        }

        public void DeletePlayer(Database.Player p)
        {
            playerList.Remove(p.PlayerPID);
        }

        public void ClearPlayerList()
        {
            playerList.Clear();
        }
    }
}
