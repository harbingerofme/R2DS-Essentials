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
    }
}
