using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;

namespace Julo.Network
{
    // TODO change name
    public class ConnectionsAndPlayers
    {
        Dictionary<int, ConnectionInfo> connections;
        Dictionary<uint, PlayerInfo> players;
        public int localConnectionNumber;

        bool isServer;

        public ConnectionsAndPlayers(bool isServer, int localConnectionNumber)
        {
            this.isServer = isServer;
            this.localConnectionNumber = localConnectionNumber;

            if(isServer != (localConnectionNumber == 0))
            {
                Log.Error("Unmatching isServer={0}, localConn={1}", isServer, localConnectionNumber);
            }

            connections = new Dictionary<int, ConnectionInfo>();
            players = new Dictionary<uint, PlayerInfo>();
        }

        // ///////// Connections

        public bool HasConnection(int connectionId)
        {
            return connections.ContainsKey(connectionId);
        }

        public void AddConnection(ConnectionInfo connection)
        {
            connections.Add(connection.connectionId, connection);
            if(connection.players.Count > 0)
            {
                Log.Error("Connection added with players");
            }
        }

        public ConnectionInfo GetConnection(int connectionId)
        {
            return connections[connectionId];
        }

        public IEnumerable<ConnectionInfo> AllConnections()
        {
            return connections.Values;
        }

        public void RemoveConnection(int id)
        {
            if(!connections.ContainsKey(id))
            {
                Log.Error("No connection with id={0}", id);
                return;
            }

            var connectionInfo = connections[id];

            foreach(var p in connectionInfo.players)
            {
                // TODO remove players?
                var playerId = p.PlayerId();
                if(!players.ContainsKey(playerId))
                {
                    Log.Error("Player not found here");
                }
                else
                {
                    // TODO do something with actual player?
                    players.Remove(playerId);
                }
            }

            connections.Remove(id);
        }

        // ///////// Players

        public IEnumerable<PlayerInfo> AllPlayers()
        {
            return players.Values;
        }
        
        public PlayerInfo GetPlayerInfo(uint playerId)
        {
            if(players.ContainsKey(playerId))
            {
                return players[playerId];
            }

            Log.Warn("PlayerInfo not found");

            return null;
        }

        public IDualPlayer GetPlayer(uint playerId)
        {
            return GetPlayerInfo(playerId)?.actualPlayer;
        }

        // player can be null
        public void AddPlayer(int connectionId, PlayerInfo playerInfo/*IDualPlayer player, DualPlayerMessage playerScreenshot*/)
        {
            if(!HasConnection(connectionId))
            {
                AddConnection(new ConnectionInfo(connectionId));
            }
            //GetConnection(connectionId).AddPlayer(player, playerScreenshot);

            GetConnection(connectionId).AddPlayer(playerInfo);

            var playerId = playerInfo.PlayerId();

            players.Add(playerId, playerInfo);
        }

        // TODO don't use every frame; cache in higher leves instead
        public T GetPlayerAs<T>(uint playerId) where T : MonoBehaviour
        {
            var playerInfo = GetPlayer(playerId);

            return playerInfo == null ? null : GetPlayerAs<T>(playerInfo);
        }

        public T GetPlayerAs<T>(PlayerInfo playerInfo) where T : MonoBehaviour
        {
            var dualPlayer = playerInfo.actualPlayer;
            if(dualPlayer == null)
            {
                Log.Error("Player not registered");
                return default;
            }

            return GetPlayerAs<T>(dualPlayer);
        }

        public T GetPlayerAs<T>(IDualPlayer dualPlayer) where T : MonoBehaviour
        {
            var mb = (MonoBehaviour)dualPlayer;

            T player = mb.GetComponent<T>();

            if(player == null)
            {
                Log.Error("Component {0} not found", typeof(T));
                return default(T);
            }

            return player;
        }

        // TODO need RemovePlayer

    } // class ConnectionsAndPlayers
    
} // namespace Julo.Network
