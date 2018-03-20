using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    class AccesPermisions
    {
        IniFile whiteListFile;
        IniFile blackListFile;
        Dictionary<uint, long> tempBlackist;
        Dictionary<uint, Connection> clientsTryConnect;//key = IP converted to int - used to check if user dont try too many connects pending

        public AccesPermisions()
        {
            whiteListFile =  new IniFile("Whitelist.ini");
            blackListFile =  new IniFile("Blacklist.ini");
            clientsTryConnect = new Dictionary<uint, Connection>();
        }

        public bool CanConnect(System.Net.IPEndPoint checkIP)
        {
            byte[] tmp = checkIP.Address.GetAddressBytes();
            Array.Reverse(tmp); // flip big-endian(network order) to little-endian
            uint intAddress = BitConverter.ToUInt32(tmp, 0);

            //If whiteList is enabled then first check if is in it
            if (Program.useWhiteList)
            {
                if (whiteListFile.ContainsKey(intAddress.ToString()))
                {
                    Output.WriteLine("AccesPermisions::CanConnect " + "IP: " + checkIP.Address.ToString() + " is in white list - > allow connection");
                    return true;
                }
                else
                {
                    Output.WriteLine("AccesPermisions::CanConnect " + "IP: " + checkIP.Address.ToString() + " isn't in white list - > close connection");
                    return false;
                }
            }
            //check if this IP is in blackList if yes then close connection
            if (Program.useBlackList)
            {
                if (blackListFile.ContainsKey(intAddress.ToString()))
                {
                    Output.WriteLine("AccesPermisions::CanConnect " + "IP: " + checkIP.Address.ToString() + " is in black list - > close connection");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            //check if this IP is in tempBlackList if yes then close connection
            if (Program.useTempBlackList)
            {
                long blockTime;
                if (tempBlackist.TryGetValue(intAddress, out blockTime))
                {
                    if ((DateTime.Now.Ticks - blockTime) > 3000000000)//5 minut
                    {
                        tempBlackist.Remove(intAddress);
                        return true;
                    }
                    else
                    {
                        Output.WriteLine("AccesPermisions::CanConnect " + "IP: " + checkIP.Address.ToString() + " is in temp blacklist - > close connection");
                        return false;
                    }
                }
            }
            return true;
        }


        public void AddToTempBlackList(System.Net.IPEndPoint blockIP)
        {
            byte[] tmp = blockIP.Address.GetAddressBytes();
            Array.Reverse(tmp); // flip big-endian(network order) to little-endian
            uint intAddress = BitConverter.ToUInt32(tmp, 0);
            tempBlackist.Add(intAddress, DateTime.Now.Ticks);
        }

    }
}
