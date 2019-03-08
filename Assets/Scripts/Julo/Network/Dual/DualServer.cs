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

        public DualContext dualContext;

        protected Mode mode;
        private DualPlayer playerModel;

        DualClient localClient = null;

        public DualServer(Mode mode, DualPlayer playerModel)
        {
            instance = this;

            this.mode = mode;
            this.playerModel = playerModel;

            dualContext = new DualContext(true, 0);
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

            localClient = client;

            dualContext.AddConnection(new ConnectionInfo(DNM.LocalConnectionId, networkConnection));
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
            dualContext.AddConnection(new ConnectionInfo(id, networkConnection));
        }


        public virtual void OnClientDisconnected(int connectionId)
        {
            RemoveClient(connectionId);
        }

        
        protected void RemoveClient(int connectionId)
        {
            if(!dualContext.HasConnection(connectionId))
            {
                Log.Error("Could not remove connection");
                return;
            }

            var conn = dualContext.GetConnection(connectionId);

            RemoveAllPlayers(conn);

            /*
            // TODO this is traversing players and removing them simultaneously, could cause problems
            foreach(var controllerAndPlayer in conn.players)
            {
                SendToAll(MsgType.RemovePlayer, new DualPlayerSnapshot(controllerAndPlayer.Value));
                dualContext.RemovePlayer(connectionId, controllerAndPlayer.Key);
            }
            */

            dualContext.RemoveConnection(connectionId);
        }

        protected void RemoveAllPlayers(ConnectionInfo conn)
        {
            var nextPlayer = conn.GetSomePlayer();

            while(nextPlayer != null)
            {
                RemovePlayer(nextPlayer);
                nextPlayer = conn.GetSomePlayer();
            }
        }

        protected void RemovePlayer(DualPlayer player)
        {
            SendToAll(MsgType.RemovePlayer, new DualPlayerSnapshot(player));
            OnPlayerRemoved(player);
            dualContext.RemovePlayer(player.ConnectionId(), player.ControllerId());
        }
        
        ///////////////////////

        // only online mode
        public virtual void WriteRemoteClientData(ListOfMessages listOfMessages)
        {
            var allPlayers = new List<DualPlayer>(dualContext.AllPlayers());

            listOfMessages.Add(new IntegerMessage(allPlayers.Count));

            foreach(DualPlayer p in allPlayers)
            {
                WritePlayer(p, listOfMessages);
            }
        }
        
        ///////////////////////
        ///
        // only server
        public ListOfMessages AddPlayer(int connectionId, short controllerId)
        {
            var player = GameObject.Instantiate<DualPlayer>(playerModel);

            bool isLocal = connectionId == dualContext.localConnectionNumber;

            player.Init(mode, isLocal, connectionId, controllerId);

            dualContext.AddPlayer(player);

            // setup initial data in server
            var listOfMessages = new ListOfMessages();

            OnPlayerAdded(player);
            WritePlayer(player, listOfMessages);

            return listOfMessages;
        }

        protected virtual void OnPlayerAdded(DualPlayer player) { /* noop */ }
        protected virtual void OnPlayerRemoved(DualPlayer player) { /* noop */ }
        
        public virtual void WritePlayer(DualPlayer player, ListOfMessages listOfMessages)
        {
            listOfMessages.Add(new DualPlayerSnapshot(player));
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
            switch(message.messageType)
            {
                case MsgType.RemovePlayer:
                    var playerMsg = message.ReadInternalMessage<DualPlayerSnapshot>();

                    var connId = playerMsg.connectionId;
                    var controllerId = playerMsg.controllerId;

                    if(dualContext.RemovePlayer(connId, controllerId))
                    {
                        // TODO send to remote only
                        SendToAll(MsgType.RemovePlayer, playerMsg);
                    }
                    else
                    {
                        Log.Error("Could not remove player {0}:{1}", connId, controllerId);
                    }
                    
                    break;

                default:
                    var msg = System.String.Format("Unhandled message number={0}", message.messageType - MsgType.Highest);
                    throw new System.Exception(msg);
                    //break;
            }
        }

    } // class DNMServer

} // namespace Julo.Network