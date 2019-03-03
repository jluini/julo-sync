using System.Collections.Generic;

using Julo.Logging;

namespace Julo.Network
{


    public class ConnectionsAndPlayers
    {

        Dictionary<int, ConnectionData> connectionData;
        bool isServer;

        public ConnectionsAndPlayers(bool isServer)
        {
            this.isServer = isServer;

            connectionData = new Dictionary<int, ConnectionData>();
        }

        public bool HasConnection(int connectionId)
        {
            return connectionData.ContainsKey(connectionId);
        }

        public ConnectionData GetConnection(int connectionId)
        {
            return connectionData[connectionId];
        }

        // TODO return Values list instead of dict?
        public Dictionary<int, ConnectionData> AllConnections()
        {
            return connectionData;
        }

        public void RemoveConnection(int id)
        {
            foreach(var p in connectionData[id].players)
            {
                // TODO remove player?
            }

            connectionData.Remove(id);
        }

        public void AddConnectionInServer(int id, ConnectionToClient connection)
        {
            if(connectionData.ContainsKey(id))
            {
                Log.Error("Already have a connection with id={0}", id);
                return;
            }

            connectionData.Add(id, new ConnectionData(id, connection));
        }

        public void AddConnectionInClient(int id)
        {
            connectionData.Add(id, new ConnectionData(id));
        }

        public bool HasAnyPlayer(uint netId)
        {
            foreach(var conn in connectionData.Values)
            {
                foreach(var p in conn.players)
                {
                    if(p.playerData.netId == netId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddPlayer(int connectionId, IDualPlayer player, DualPlayerMessage playerData, MessageStackMessage stack)
        {
            if(!HasConnection(connectionId))
            {
                AddConnectionInClient(connectionId);
            }
            GetConnection(connectionId).AddPlayer(player, playerData, stack);
        }

        public PlayerData GetPlayerIfAny(uint netId)
        {
            foreach(var conn in connectionData.Values)
            {
                foreach(var p in conn.players)
                {
                    if(p.playerData.netId == netId)
                    {
                        return p;
                    }
                }
            }
            
            return null;
        }
    } // class ConnectionsAndPlayers

    public class ConnectionData
    {
        public int connectionId;
        public List<PlayerData> players;
        
        // if hosted
        public ConnectionToClient connectionToClient;

        // in server
        public ConnectionData(int connectionId, ConnectionToClient connection)
        {
            this.connectionId = connectionId;
            connectionToClient = connection;

            this.players = new List<PlayerData>();
        }

        // in remote client
        public ConnectionData(int connectionId)
        {
            this.connectionId = connectionId;
            connectionToClient = null;
            this.players = new List<PlayerData>();
        }

        // in server
        public void AddPlayer(IDualPlayer player)
        {
            players.Add(new PlayerData(player));
        }

        /// <summary>
        ///     In client.
        ///     actualPlayer could be null if not started yet
        /// </summary>
        public void AddPlayer(IDualPlayer actualPlayer, DualPlayerMessage dualPlayerData, MessageStackMessage stack)
        {
            players.Add(new PlayerData(actualPlayer, dualPlayerData, stack));
        }
        
    }
    
    public class PlayerData
    {
        public IDualPlayer actualPlayer;
        public DualPlayerMessage playerData;
        public MessageStackMessage stack;

        // in server
        public PlayerData(IDualPlayer player)
        {
            this.actualPlayer = player;
            playerData = new DualPlayerMessage(player);
            stack = null;
        }

        // in client
        public PlayerData(IDualPlayer actualPlayer, DualPlayerMessage playerData, MessageStackMessage stack)
        {
            this.actualPlayer = actualPlayer;
            if(actualPlayer != null)
            {
                Log.Debug("Player data starts synchronized");
            }
            else
            {
                Log.Debug("Waiting to resolve player with netId={0}", playerData.netId);
            }

            this.playerData = playerData;
            this.stack = stack;
        }
    }

} // namespace Julo.Network
