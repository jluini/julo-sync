using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;

namespace Julo.Network
{
    // TODO change name
    public class ConnectionsAndPlayers
    {
        Dictionary<int, ConnectionInfo> connections;
        //Dictionary<uint, PlayerInfo> players;
        //Dictionary<uint, PlayerInfo> players;
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
            //players = new Dictionary<uint, PlayerInfo>();
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
        
        // only server
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
                // TODO remove players
                /*
                var playerId = p.PlayerId();
                if(!players.ContainsKey(playerId))
                {
                    Log.Error("Player not found here");
                }
                else
                {
                    players.Remove(playerId);
                }
                */
            }

            connections.Remove(id);
        }

        // ///////// Players

        public IEnumerable<DualPlayer> AllPlayers()
        {
            var ret = new List<DualPlayer>();

            foreach(var c in connections.Values)
            {
                foreach(var p in c.players.Values)
                {
                    ret.Add(p);
                }
            }

            return ret;
        }

        public DualPlayer GetPlayer(int connectionId, short controllerId)
        {
            if(!connections.ContainsKey(connectionId))
            {
                Log.Error("No connection {0}", connectionId);
                return default;
            }
            var connection = connections[connectionId];

            if(!connection.players.ContainsKey(controllerId))
            {
                Log.Error("No player {1} in connection {0}", connectionId, controllerId);
                return default;
            }

            var ret = connection.players[controllerId];
            return ret;
        }

        public T GetPlayerAs<T>(int connectionId, short controllerId) where T : MonoBehaviour
        {
            var dualPlayer = GetPlayer(connectionId, controllerId);

            if(dualPlayer == null)
            {
                Log.Debug("Returning null here");
                return default; 
            }

            return DNM.GetPlayerAs<T>(dualPlayer);
        }

        public IEnumerable<DualPlayer> GetPlayers(int connectionId)
        {
            if(!connections.ContainsKey(connectionId))
            {
                Log.Error("No connection {0}", connectionId);
                return new List<DualPlayer>();
            }

            var connection = connections[connectionId];

            return connection.GetPlayers();
        }

        public List<T> GetPlayersAs<T>(int connectionId) where T : MonoBehaviour
        {
            if(!connections.ContainsKey(connectionId))
            {
                Log.Error("No connection {0}", connectionId);
                return default;
            }

            var connection = connections[connectionId];

            return connection.GetPlayersAs<T>();
        }

        public void AddPlayer(DualPlayer player)
        {
            var connectionId = player.ConnectionId();
            var controllerId = player.ControllerId();

            if(!HasConnection(connectionId))
            {
                if(isServer)
                {
                    Log.Warn("I'm server and connection was not added before player??");
                }
                AddConnection(new ConnectionInfo(connectionId));
            }

            GetConnection(connectionId).AddPlayer(player);
        }

    } // class ConnectionsAndPlayers
    
} // namespace Julo.Network
