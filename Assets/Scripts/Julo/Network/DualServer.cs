using System.Collections.Generic;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;

namespace Julo.Network
{
    
    public class DualServer
    {
        public static DualServer instance = null;

        public ConnectionsAndPlayers connections;

        protected Mode mode;

        DualClient localClient = null;

        public DualServer(Mode mode)
        {
            instance = this;

            this.mode = mode;

            //this.connections = new Dictionary<int, ConnectionToClient>();
            connections = new ConnectionsAndPlayers(true);
        }

        public void AddLocalClient(DualClient client, ConnectionToClient connection)
        {
            if(client == null)
            {
                Log.Error("client is null");
                return;
            }
            if(connection == null)
            {
                Log.Error("conection is null");
                return;
            }
            if(localClient != null)
            {
                Log.Error("Already have a local client");
                return;
            }

            var id = connection.ConnectionId();

            if(id != DNM.LocalConnectionId)
            {
                Log.Error("Unexpected connectionId={0}", id);
            }

            this.localClient = client;

            //this.connections.Add(id, connection);
            this.connections.AddConnectionInServer(id, connection);
        }

        public void AddRemoteClient(ConnectionToClient connection)
        {
            // TODO checks?
            if(connection == null)
            {
                Log.Error("conection is null");
                return;
            }

            var id = connection.ConnectionId();
            //connections.Add(id, connection);
            this.connections.AddConnectionInServer(id, connection);
        }

        public void RemoveClient(int connectionId)
        {
            connections.RemoveConnection(connectionId);
        }

        /*protected virtual bool AcceptsRemoteClient()
        {
            // TODO accept criteria
            return true;
        }*/

        ///////////////////////

        // only online mode
        public virtual void WriteRemoteClientData(List<MessageBase> messageStack)
        {
            var allPlayers = new List<IDualPlayer>();

            foreach(var c in connections.AllConnections().Values)
            {
                foreach(var playerData in c.players)
                {
                    var p = playerData.actualPlayer;

                    if(p.ConnectionId() != c.connectionId)
                    {
                        Log.Warn("Wrong data");
                    }
                    allPlayers.Add(p);
                }
            }
            messageStack.Add(new IntegerMessage(allPlayers.Count));

            foreach(IDualPlayer p in allPlayers)
            {
                WritePlayer(p, messageStack);
            }
        }
        
        ///////////////////////
        ///
        // only server
        public List<MessageBase> AddPlayer(IDualPlayer player)
        {
            //connections[player.ConnectionId()].AddPlayer(player);

            connections.GetConnection(player.ConnectionId()).AddPlayer(player);

            // setup initial data in server
            var messageStack = new List<MessageBase>();

            // Log.Debug("Adding DualPlayer({0} = {1}:{2}) to stack", netId, connectionId, controllerId);

            OnPlayerAdded(player);
            WritePlayer(player, messageStack);

            return messageStack;
        }

        // only server
        public virtual void OnPlayerAdded(IDualPlayer player)
        {
            // noop
        }

        // only server
        public virtual void WritePlayer(IDualPlayer player, List<MessageBase> messageStack)
        {
            messageStack.Add(new DualPlayerMessage(player));
        }

        ///////////////////////

        // sending messages to clients
        // TODO tratar de no recibirlo wrapped
        // TODO usar polimorfismo en vez de if...

        protected void SendTo(int destinationId, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(destinationId != DNM.LocalConnectionId)
                {
                    Log.Warn("Invalid connectionId != {0} in offline mode: {1}", DNM.LocalConnectionId, destinationId);
                    return;
                }

                localClient.SendMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendTo(destinationId, msgType, msg);
            }
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                localClient.SendMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
            }
        }

        protected void SendToAllBut(int connectionId, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(connectionId != 0)
                {
                    Log.Warn("Unexpected 'but' id {0} in offline mode", connectionId);
                }
                // sending to nobody
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAllBut(connectionId, msgType, msg);
            }
        }

        // message handling

        public void SendMessage(WrappedMessage message, int from)
        {
            OnMessage(message, from);
        }


        protected virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        /*
        protected virtual void OnStartServer() { }

        // only online mode
        public virtual void WriteInitialData(List<MessageBase> messages)
        {
            messages.Add(new UnityEngine.Networking.NetworkSystem.IntegerMessage(numRoles));
        }

        public abstract void StartGame();

        ///////// Messaging
        

        protected void SendTo(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendTo(who, msgType, msg);
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...
            if(mode == Mode.OfflineMode)
            {
                GameClient.instance.OnMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
            }
        }

        protected void SendToAllBut(int who, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(who != 0)
                {
                    Log.Warn("Unexpected 'but' id {0} in offline mode", who);
                }
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAllBut(who, msgType, msg);
            }
        }

        // only online mode
        public virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        ///////// Utils
        
        protected List<Player> GetPlayersForRole(int role)
        {
            return playersPerRole[role - 1];
        }
        */
    } // class DNMServer

} // namespace Julo.Network