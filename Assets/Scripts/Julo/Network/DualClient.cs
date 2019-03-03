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

        // only remote (quasi)
        public void OnNewPlayerMessage(MessageStackMessage messageStack)
        {
            if(!isHosted)
            {
                var dualPlayerMessage = messageStack.ReadMessage<DualPlayerMessage>();
                var netId = dualPlayerMessage.netId;

                ReadPlayer(dualPlayerMessage, messageStack);

                // Log.Debug("NEW PLAYER id={0}", netId);

                OnlineDualPlayer registeredPlayer = null;
                if(pendingPlayers.ContainsKey(netId))
                {
                    registeredPlayer = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);

                    ResolvePlayer(registeredPlayer, dualPlayerMessage);
                }
                
                clientConnections.AddPlayer(dualPlayerMessage.connectionId, registeredPlayer, dualPlayerMessage, messageStack);
            }
        }


        public void StartOnlinePlayer(OnlineDualPlayer player)
        {
            if(!isHosted)
            {
                uint netId = player.netId.Value;

                //Log.Debug("START PLAYER id={0}", netId);

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
        }

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

} // namespace Julo.Network