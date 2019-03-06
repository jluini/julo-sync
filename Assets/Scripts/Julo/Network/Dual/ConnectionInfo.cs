using System.Collections.Generic;

using UnityEngine.Networking;

namespace Julo.Network
{
    public class ConnectionInfo
    {
        // id of the connection in the server
        public int connectionId;

        //public bool isLocal;

        // only server in online mode
        public NetworkConnection networkConnection;

        public List<PlayerInfo> players;

        // in server in online mode
        public ConnectionInfo(int connectionId, NetworkConnection networkConnection)
        {
            this.connectionId = connectionId;
            //this.isLocal = connectionId == 0;
            this.networkConnection = networkConnection;
            this.players = new List<PlayerInfo>();
        }

        // TODO what about offline mode

        // in remote client
        public ConnectionInfo(int connectionId/*, bool isLocal*/)
        {
            this.connectionId = connectionId;
            //this.isLocal = isLocal;

            networkConnection = null;

            this.players = new List<PlayerInfo>();
        }

        public void AddPlayer(PlayerInfo newPlayer)
        {
            players.Add(newPlayer);
        }

        public void RemovePlayer(PlayerInfo playerInfo)
        {
            players.Remove(playerInfo);
        }

    }

} // namespace Julo.Network
