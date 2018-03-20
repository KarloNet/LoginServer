using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace LoginServer
{
    static class Users
    {
        //static ConcurrentDictionary<int, Connection> logUser = new ConcurrentDictionary<int, Connection>();
        static MultiKeyDictionary<Connection> logUser = new MultiKeyDictionary<Connection>();

        public static bool Add(int UID, Connection conn)
        {
            return logUser.Add(UID, conn.TokenID, conn);
        }

        public static bool Exists(int UID)
        {
            return logUser.ContainsKey(UID);
        }

        public static void Remove(int UID, int connectionToken)
        {
            logUser.Remove(UID, connectionToken);
        }

        public static void Remove(int UID, out Connection val)
        {
            logUser.Remove(UID, out val);
        }

        public static int Count
        {
            get
            {
                return logUser.baseDictionary.Count;
            }
        }
    }
}
