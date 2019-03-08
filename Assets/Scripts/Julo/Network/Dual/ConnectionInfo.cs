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

        public Dictionary<short, DualPlayer> players;

        // in server
        public ConnectionInfo(int connectionId, NetworkConnection networkConnection)
        {
            this.connectionId = connectionId;
            this.networkConnection = networkConnection;
            this.players = new Dictionary<short, DualPlayer>();
        }

        // in remote client
        public ConnectionInfo(int connectionId)
        {
            this.connectionId = connectionId;
            this.networkConnection = null;
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

        public DualPlayer GetSomePlayer()
        {
            if(players.Count == 0)
            {
                return null;
            }
            var playersEnum = players.GetEnumerator();
            playersEnum.MoveNext();
            return playersEnum.Current.Value;
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

    } // class ConnectionInfo

} // namespace Julo.Network
