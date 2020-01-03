using RoR2;
using UnityEngine.Networking;

namespace R2DSEssentials.Util
{
    internal class Networking
    {
        internal static NetworkUser FindNetworkUserForConnectionServer(NetworkConnection connection)
        {
            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                if (networkUser.connectionToClient == connection)
                {
                    return networkUser;
                }
            }

            return null;
        }

        internal static NetworkConnection FindNetworkConnectionFromNetworkUser(NetworkUser networkUser)
        {
            foreach (var connection in NetworkServer.connections)
            {
                if (networkUser.connectionToClient == connection)
                {
                    return connection;
                }
            }

            return null;
        }

        internal static NetworkUser GetNetworkUserFromSteamId(ulong steamId)
        {
            foreach (var networkUser in NetworkUser.readOnlyInstancesList)
            {
                if (networkUser.GetNetworkPlayerName().steamId.value == steamId)
                {
                    return networkUser;
                }
            }

            return null;
        }

        internal static int GetPlayerIndexFromNetworkUser(NetworkUser networkUser)
        {
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                if (networkUser.Network_id.Equals(NetworkUser.readOnlyInstancesList[i].Network_id))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
