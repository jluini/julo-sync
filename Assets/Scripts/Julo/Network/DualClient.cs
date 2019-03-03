using System.Collections.Generic;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;

namespace Julo.Network
{
    public class DualClient
    {
        public static DualClient instance = null;

        bool isInitialized = false; // TODO why this?

        protected Mode mode;
        protected bool isHosted;

        protected DualServer server;

        // only remote client
        Dictionary<uint, OnlineDualPlayer> pendingPlayers = new Dictionary<uint, OnlineDualPlayer>();

        // only remote
        ConnectionsAndPlayers clientConnections;

        ConnectionsAndPlayers connections
        {
            get
            {
                if(isHosted)
                {
                    return DualServer.instance.connections;
                }
                else
                {
                    return clientConnections;
                }
            }
        }

        /// <summary>
        ///     Creates client.
        /// </summary>
        /// <param name="mode">Offline or online mode</param>
        /// <param name="server">
        ///     If server == null, it's a remote client and mode must be online.
        ///     If server != null it's a hosted client in either offline or online mode.
        /// </param>
        public DualClient(Mode mode, DualServer server = null)
        {
            instance = this;
            this.mode = mode;
            this.server = server;
            isHosted = server != null;

            isInitialized = false;

            if(!isHosted)
            {
                clientConnections = new ConnectionsAndPlayers(false);
            }

            if(!isHosted && mode == Mode.OfflineMode)
            {
                Log.Error("Non-hosted client not allowed in offline mode");
            }
        }

        // only remote
        public virtual void InitializeState(MessageStackMessage messageStack)
        {
            var numPlayersMessage = messageStack.ReadMessage<IntegerMessage>();
            var numPlayers = numPlayersMessage.value;

            //Log.Debug("Detectados {0} players preexistentes", numPlayers);

            for(int i = 0; i < numPlayers; i++)
            {

                var dualPlayerMsg = messageStack.ReadMessage<DualPlayerMessage>();

                ReadPlayer(dualPlayerMsg, messageStack);

                var netId = dualPlayerMsg.netId;
                var connId = dualPlayerMsg.connectionId;
                var controllerId = dualPlayerMsg.controllerId;

                Log.Debug("INITIALIZE STATE {0} = {1}/{2}", netId, connId, controllerId);

                OnlineDualPlayer registeredPlayer = null;

                if(pendingPlayers.ContainsKey(netId))
                {
                    registeredPlayer = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);

                    //ResolvePlayer(registeredPlayer, dualPlayerMsg, messageStack);
                    ResolvePlayer(registeredPlayer, dualPlayerMsg);
                }
                //else
                //{
                    //pendingMessages.Add(netId, new PendingPlayerStack(dualPlayerMsg, messageStack));
                //}

                //clientConnections.GetConnection(connId).AddPlayer(registeredPlayer, dualPlayerMsg, messageStack);
                clientConnections.AddPlayer(connId, registeredPlayer, dualPlayerMsg, messageStack);
            }
        }

        /*// only remote
        public virtual void OnPlayerResolved(OnlineDualPlayer player, MessageStackMessage messageStack)
        {
            // noop
        }*/

        // only remote (quasi)
        public void OnNewPlayerMessage(MessageStackMessage messageStack)
        {
            if(!isHosted)
            {
                var dualPlayerMessage = messageStack.ReadMessage<DualPlayerMessage>();
                var netId = dualPlayerMessage.netId;

                ReadPlayer(dualPlayerMessage, messageStack);

                Log.Debug("NEW PLAYER id={0}", netId);

                // Log.Debug("Received message netId={0}, connId={1}, controller={2}", netId, dualPlayerMessage.connectionId, dualPlayerMessage.controllerId);

                OnlineDualPlayer registeredPlayer = null;
                if(pendingPlayers.ContainsKey(netId))
                {
                    registeredPlayer = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);

                    ResolvePlayer(registeredPlayer, dualPlayerMessage);
                }
                //else
                //{
                //    pendingMessages.Add(netId, new PendingPlayerStack(dualPlayerMessage, messageStack));
                //}
                
                clientConnections.AddPlayer(dualPlayerMessage.connectionId, registeredPlayer, dualPlayerMessage, messageStack);
            }
        }


        public void StartOnlinePlayer(OnlineDualPlayer player)
        {
            if(!isHosted)
            {
                uint netId = player.netId.Value;

                Log.Debug("START PLAYER id={0}", netId);

                //bool isPendingMessage = pendingMessages.ContainsKey(netId);
                //bool isPendingToRegister = connections.HasAnyPlayer(netId);
                PlayerData playerData = connections.GetPlayerIfAny(netId);
                bool isRegistered = playerData != null;

                if(isRegistered)
                {
                    ResolvePlayer(player, playerData.playerData);
                }
                else
                {
                    pendingPlayers.Add(netId, player);
                }


                /*
                if(pendingMessages.ContainsKey(thisId))
                {
                    OnPlayerRegistered(player, pendingMessages[thisId]);
                    pendingMessages.Remove(thisId);
                }
                else
                {
                    Log.Warn("No message for registering this player netId={0}", thisId);
                }
                */
            }
        }

        public virtual void ReadPlayer(DualPlayerMessage dualPlayer, MessageStackMessage messageStack)
        {
            // noop
        }

        public virtual void ResolvePlayer(OnlineDualPlayer player, DualPlayerMessage dualPlayer/*, MessageStackMessage messageStack*/)
        {
            // Log.Debug("Resolving!!! netId={0}, player={1}, conn={2}/{3}", player.NetworkId(), player, dualPlayer.connectionId, dualPlayer.controllerId);

            var netId = dualPlayer.netId;
            var connId = dualPlayer.connectionId;
            var controllerId = dualPlayer.controllerId;

            if(player.ConnectionId() == connId)
                Log.Warn("Connection id already set to {0}", connId);
            if(player.ControllerId() == controllerId)
                Log.Warn("Controller id already set to {0}", controllerId);

            player.Init(connId, controllerId);

            //OnPlayerResolved(player, messageStack);
        }

        /*
        public virtual void OnPlayerRegistered(OnlineDualPlayer player, MessageStackMessage message)
        {
            var dualPlayerMessage = message.ReadInitialMessage<DualPlayerMessage>();

            uint netId = dualPlayerMessage.netId;
            int connectionId = dualPlayerMessage.connectionId;
            short controllerId = dualPlayerMessage.controllerId;

            Log.Debug("Se quiere setear DualPlayer({0} = {1}:{2}) a netId={3}", netId, connectionId, controllerId, player.netId.Value);

            if(player.netId.Value != netId)
            {
                Log.Warn("Unmatching last message");
                return;
            }

            player.connectionId = connectionId;
            player.controllerId = controllerId;
        }
        */
        ////
        /*
        public void OnConnectAsLocal(NetworkConnection conn)
        {
            // TODO set ready here?
            ClientScene.Ready(conn);
            AddPlayer(0);
        }

        public void OnConnectAsRemote(NetworkConnection conn)
        {
            conn.RegisterHandler(MsgType.InitialStatus, OnClientInitialStatusMessage);
            // Waiting for InitialStatus message
            Log.Debug("Waiting for InitialStatus message");
        }

        // only in non-hosted clients
        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {
            // TODO necessary to receive if rejected?

            var msg = messageReader.ReadMessage<MessageStackMessage>();

            Log.Debug("Received InitialStatus message: {0}, {1}", msg.accepted, msg.count);

            if(!msg.accepted)
            {
                Log.Warn("I was rejected :(");
                return;
            }

            // creates non-hosted client
            dualClient = remoteClientDelegate(msg);


            SetState(DNMState.Client);
            CreatePlayer(localClient.connection); // create remote initial player

            /*
            InstantiateClientAsync(msg, () =>
            {
                SetState(DNMState.Client);
                CreatePlayer(localClient.connection); // create remote initial player
            });
            * /
        }

        void AddPlayer(short playerControllerId)
        {
            DualNetworkManager.instance.AddPlayerCommand(playerControllerId);
        }
        */

        // sending messages to server
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...
        protected void SendToServer(short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                server.SendMessage(new WrappedMessage(msgType, msg), 0);
            }
            else
            {
                DualNetworkManager.instance.GameClientSendToServer(msgType, msg);
            }
        }

        // message handling

        // send a message to this client
        public void SendMessage(WrappedMessage message)
        {
            OnMessage(message);
        }

        protected virtual void OnMessage(WrappedMessage message)
        {
            throw new System.Exception("Unhandled message");
        }


    } // class DualClient
    /*
    public class PendingPlayerStack
    {
        public DualPlayerMessage dualPlayerData;
        public MessageStackMessage stack;

        public PendingPlayerStack(DualPlayerMessage dualPlayerData, MessageStackMessage stack)
        {
            this.dualPlayerData = dualPlayerData;
            this.stack = stack;
        }
    }
    */

} // namespace Julo.Network