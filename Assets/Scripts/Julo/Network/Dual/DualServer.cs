using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;

namespace Julo.Network
{
    
    public class DualServer
    {
        public static DualServer instance = null;

        // TODO rename class
        // TODO rename instance
        public ConnectionsAndPlayers connections;

        protected Mode mode;
        private DualPlayer playerModel;

        DualClient localClient = null;

        public DualServer(Mode mode, DualPlayer playerModel)
        {
            instance = this;

            this.mode = mode;
            this.playerModel = playerModel;

            connections = new ConnectionsAndPlayers(true, 0);
        }

        public void AddLocalClient(DualClient client, NetworkConnection networkConnection)
        {
            if(client == null)
            {
                Log.Error("client is null");
                return;
            }
            if(localClient != null)
            {
                Log.Error("Already have a local client");
                return;
            }

            if(mode == Mode.OnlineMode)
            {
                if(networkConnection == null)
                {
                    Log.Error("conection is null");
                    return;
                }

                var id = networkConnection.connectionId;
                if(id != DNM.LocalConnectionId)
                {
                    Log.Error("DualServer::AddLocalClient: unexpected connectionId={0}", id);
                }
            }

            this.localClient = client;
            this.connections.AddConnection(new ConnectionInfo(DNM.LocalConnectionId, networkConnection));
        }

        public void AddRemoteClient(NetworkConnection networkConnection)
        {
            // TODO checks?
            if(networkConnection == null)
            {
                Log.Error("connection is null");
                return;
            }

            var id = networkConnection.connectionId;
            this.connections.AddConnection(new ConnectionInfo(id, networkConnection));
        }
        
        public void RemoveClient(int connectionId)
        {
            if(connectionId == DNM.LocalConnectionId)
            {
                Log.Warn("Removing local connection");
            }
            // TODO

            foreach(var player in connections.GetConnection(connectionId).players.Values)
            {
                //OnPlayerRemoved(player);
            }

            connections.RemoveConnection(connectionId);
        }
        
        ///////////////////////

        // only online mode
        public virtual void WriteRemoteClientData(List<MessageBase> messageStack)
        {
            var allPlayers = new List<DualPlayer>(connections.AllPlayers()); //new List<DualPlayer>();

            messageStack.Add(new IntegerMessage(allPlayers.Count));

            foreach(DualPlayer p in allPlayers)
            {
                WritePlayer(p, messageStack);
            }
        }
        
        ///////////////////////
        ///
        // only server
        public List<MessageBase> AddPlayer(int connectionId, short controllerId) //DualPlayer player)
        {
            var player = GameObject.Instantiate<DualPlayer>(playerModel);

            bool isLocal = connectionId == connections.localConnectionNumber;

            player.Init(mode, connectionId, controllerId, isLocal);

            //var playerInfo = new PlayerInfo(player);

            connections.AddPlayer(player);
            
            // setup initial data in server
            var messageStack = new List<MessageBase>();

            OnPlayerAdded(player);
            WritePlayer(player, messageStack);

            return messageStack;
        }

        public virtual void OnPlayerAdded(DualPlayer player)
        {
            // noop
        }
        
        public virtual void WritePlayer(DualPlayer player, List<MessageBase> messageStack)
        {
            messageStack.Add(new PlayerMessage(player));
        }

        ///////////////////////

        // ///////// Messaging

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
            var msg = System.String.Format("Unhandled message number={0}", message.messageType - MsgType.Highest);
            throw new System.Exception(msg);
        }

    } // class DNMServer

} // namespace Julo.Network