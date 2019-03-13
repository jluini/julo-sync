using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    public class DualContext
    {
        public static DualContext instance;

        Dictionary<int, ConnectionInfo> connections;

        public int localConnectionNumber;

        bool isServer;

        public DualContext(bool isServer, int localConnectionNumber)
        {
            instance = this;

            this.isServer = isServer;
            this.localConnectionNumber = localConnectionNumber;

            if(isServer != (localConnectionNumber == 0))
            {
                Log.Error("Unmatching isServer={0}, localConn={1}", isServer, localConnectionNumber);
            }

            connections = new Dictionary<int, ConnectionInfo>();
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
            if(!isServer)
            {
                Log.Error("Invalid in remote client");
                return;
            }
            if(!connections.ContainsKey(id))
            {
                Log.Error("No connection with id={0}", id);
                return;
            }

            var conn = connections[id];
            if(conn.players.Count > 0)
            {
                Log.Warn("There are still {0} players!!", conn.players.Count);
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

        public DualPlayer GetPlayer(DualPlayerSnapshot snapshot)
        {
            var connId = snapshot.connectionId;
            var contId = snapshot.controllerId;

            if(connId < 0)
            {
                if(connId != -1 || contId != -1)
                {
                    Log.Warn("Invalid values");
                }
                return null;
            }

            return GetPlayer(connId, contId);
        }

        public DualPlayer GetPlayer(int connectionId, short controllerId)
        {
            if(!connections.ContainsKey(connectionId))
            {
                Log.Error("No connection {0}", connectionId);
                return null;
            }
            var connection = connections[connectionId];

            if(!connection.players.ContainsKey(controllerId))
            {
                Log.Error("No player {1} in connection {0}", connectionId, controllerId);
                return null;
            }

            var ret = connection.players[controllerId];
            return ret;
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

        public bool RemovePlayer(int connectionId, short controllerId)
        {
            if(!connections.ContainsKey(connectionId))
            {
                Log.Error("No connection {0}", connectionId);
                return false;
            }
            var connection = connections[connectionId];

            if(!connection.players.ContainsKey(controllerId))
            {
                Log.Error("No player {1} in connection {0}", connectionId, controllerId);
                return false;
            }

            var player = connection.players[controllerId];

            connection.players.Remove(controllerId);

            if(connection.players.Count == 0)
            {
                Log.Debug("Connection {0} has zero players now", connectionId);

                if(!isServer)
                {
                    connections.Remove(connectionId);
                }
            }

            GameObject.Destroy(player.gameObject);

            return true;
        }

    } // class DualContext
    
} // namespace Julo.Network
