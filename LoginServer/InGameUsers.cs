using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace LoginServer
{
    static class InGameUsers
    {
        public class GameUser
        {
            int userID;
            int playerID;
            int gameServer;

            public GameUser(int userID, int playerID, int gameServer)
            {
                this.userID = userID;
                this.playerID = playerID;
                this.gameServer = gameServer;
            }
            public int UserID { get { return userID; } set { userID = value; } }
            public int PlayerID { get { return playerID; } set { playerID = value; } }
            public int GameServer { get { return gameServer; } set { gameServer = value; } }
        }

        //static MultiKeyDictionary<GameUser> gameUser = new MultiKeyDictionary<GameUser>();
        //static ConcurrentDictionary<int, GameUser> gameUser = new ConcurrentDictionary<int, GameUser>();
        static ConcurrentDictionary<int, ConcurrentDictionary<int, GameUser>> gameServerList = new ConcurrentDictionary<int, ConcurrentDictionary<int, GameUser>>();
       
        public static bool Add(int UID, int playerID, int GameServerID)
        {
            GameUser gUser = new GameUser(UID, playerID, GameServerID);
            if (gameServerList.ContainsKey(GameServerID))
            {
                Output.WriteLine("InGameUsers::Add Add user to existng InGameUsers list of server id: " + GameServerID.ToString());
                ConcurrentDictionary<int, GameUser> gUserList;
                gameServerList.TryGetValue(GameServerID, out gUserList);
                return gUserList.TryAdd(UID, gUser);
            }
            else
            {
                Output.WriteLine("InGameUsers::Add Add user to NEW InGameUsers list. Server id: " + GameServerID.ToString());
                ConcurrentDictionary<int, GameUser> gUserList = new ConcurrentDictionary<int,GameUser>();
                gUserList.TryAdd(UID, gUser);
                return gameServerList.TryAdd(GameServerID, gUserList);
            }
        }

        public static bool Exists(int UID)
        {
            bool exist = false;
            foreach (int key in gameServerList.Keys.ToList())
            {
                exist = gameServerList[key].ContainsKey(UID);
                if (exist)
                {
                    return true;
                }
            }
            return exist;
        }

        public static int UsersCount()
        {
            int count = 0;
            foreach (int key in gameServerList.Keys.ToList())
            {
                count += gameServerList[key].Count;
            }
            return count;
        }

        public static GameUser Remove(int UID)
        {
            GameUser gUser = null;
            foreach (int key in gameServerList.Keys.ToList())
            {
                gameServerList[key].TryRemove(UID, out gUser);
                if (gUser != null)
                {
                    return gUser;
                }
            }
            return gUser;
        }

        public static ConcurrentDictionary<int, GameUser> RemoveGameServer(int gameServerID)
        {
            ConcurrentDictionary<int, GameUser> usersList = null;
            gameServerList.TryRemove(gameServerID, out usersList);
            if (usersList != null)
            {
                Output.WriteLine("InGameUsers::RemoveGameServer Game server ID: " + gameServerID.ToString() + " removed with " + usersList.Count + " users inside");
            }
            else
            {
                Output.WriteLine("InGameUsers::RemoveGameServer Thers no game server with ID: " + gameServerID.ToString());
            }
            return usersList;
        }
    }
}
