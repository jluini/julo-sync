using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    public class ConnectionInfo
    {
        // id of the connection in the server
        public int connectionId;

        //public bool isLocal;

        // only server in online mode
        public NetworkConnection networkConnection;

        //public List<PlayerInfo> players;
        //public List<DualPlayer> players;
        public Dictionary<short, DualPlayer> players;


        // in server in online mode
        public ConnectionInfo(int connectionId, NetworkConnection networkConnection)
        {
            this.connectionId = connectionId;
            //this.isLocal = connectionId == 0;
            this.networkConnection = networkConnection;
            this.players = new Dictionary<short, DualPlayer>();
        }

        // TODO what about offline mode

        // in remote client
        public ConnectionInfo(int connectionId/*, bool isLocal*/)
        {
            this.connectionId = connectionId;
            //this.isLocal = isLocal;

            networkConnection = null;

            this.players = new Dictionary<short, DualPlayer>();
        }

        public void AddPlayer(DualPlayer newPlayer)
        {
            var connId = newPlayer.ConnectionId();
            var controllerId = newPlayer.ControllerId();

            if(newPlayer.ConnectionId() != connectionId)
            {
                Log.Error("That player is not mine");
            }

            if(players.ContainsKey(controllerId))
            {
                Log.Error("Already have a player {0}:{1}", connId, controllerId);
                return;
            }

            players.Add(controllerId, newPlayer);
        }

        public void RemovePlayer(DualPlayer dualPlayer)
        {
            if(!players.Remove(dualPlayer.ControllerId()))
            {
                Log.Error("Player not found");
            }
        }

        public IEnumerable<DualPlayer> GetPlayers()
        {
            return players.Values;
        }

        public List<T> GetPlayersAs<T>() where T : MonoBehaviour
        {
            var ret = new List<T>();

            foreach(var p in players.Values)
            {
                ret.Add(DNM.GetPlayerAs<T>(p));
            }

            return ret;
        }

    }

} // namespace Julo.Network
