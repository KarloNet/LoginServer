using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    static class Worlds
    {
        static List<WorldConnectionListener.GameServer> gServerList;

        public static void SetList(List<WorldConnectionListener.GameServer> gameServerList)
        {
            gServerList = gameServerList;
        }
        //return server that handle users at given position
        public static WorldConnectionListener.GameServer GetServer(int xPos, int yPos)
        {
            int sCount;
            if (gServerList != null) sCount = gServerList.Count; else return null;
            for(int i = sCount - 1; i >= 0; i--)
            {
                WorldConnectionListener.GameServer gs = gServerList[i];
                if(xPos >= gs.StartX && xPos <= gs.EndX && yPos >= gs.StartY && yPos <= gs.EndY) return gs;
            }
            return null;
        }

    }
}
