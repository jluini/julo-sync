using System.Collections.Generic;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;

namespace Julo.Network
{
    public class DualClient
    {
        public static DualClient instance = null;

        protected Mode mode;
        protected bool isHosted;

        protected DualServer server;

        // only remote client
        Dictionary<uint, OnlineDualPlayer> pendingPlayers = new Dictionary<uint, OnlineDualPlayer>();

        // only remote
        ConnectionsAndPlayers clientConnections;

        protected ConnectionsAndPlayers connections
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

            for(int i = 0; i < numPlayers; i++)
            {
                var dualPlayerMsg = messageStack.ReadMessage<DualPlayerMessage>();

                ReadPlayer(dualPlayerMsg, messageStack);

                var netId = dualPlayerMsg.playerId;
                var connId = dualPlayerMsg.connectionId;
                var controllerId = dualPlayerMsg.controllerId;

                OnlineDualPlayer registeredPlayer = null;

                if(pendingPlayers.ContainsKey(netId))
                {
                    registeredPlayer = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);
                }

                clientConnections.AddPlayer(connId, registeredPlayer, dualPlayerMsg);

                if(registeredPlayer != null)
                {
                    ResolvePlayer(registeredPlayer, dualPlayerMsg);
                }
            }
        }

        public void OnNewPlayerMessage(MessageStackMessage messageStack)
        {
            if(!isHosted)
            {
                var dualPlayerMessage = messageStack.ReadMessage<DualPlayerMessage>();
                var netId = dualPlayerMessage.playerId;

                ReadPlayer(dualPlayerMessage, messageStack);

                OnlineDualPlayer registeredPlayer = null;
                if(pendingPlayers.ContainsKey(netId))
                {
                    registeredPlayer = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);
                }

                clientConnections.AddPlayer(dualPlayerMessage.connectionId, registeredPlayer, dualPlayerMessage);

                if(registeredPlayer != null)
                {
                    ResolvePlayer(registeredPlayer, dualPlayerMessage);
                }
            }
        }


        public void StartOnlinePlayer(OnlineDualPlayer player)
        {
            if(!isHosted)
            {
                uint netId = player.netId.Value;

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

        public virtual void ReadPlayer(DualPlayerMessage dualPlayerData, MessageStackMessage messageStack)
        {
            // noop
        }

        public virtual void ResolvePlayer(OnlineDualPlayer player, DualPlayerMessage dualPlayerData)
        {
            var playerId = player.PlayerId();
            var p = connections.GetPlayerIfAny(playerId);

            if(p == null)
            {
                Log.Error("Unregistered player {0}", playerId);
                return;
            }

            if(p.actualPlayer == null)
            {
                p.actualPlayer = player;
            }
            else
            {
                Log.Warn("Actual player already was set");
                if(player != p.actualPlayer)
                {
                    Log.Warn("    and to something different!!!");
                }
            }

            var netId = dualPlayerData.playerId;
            var connId = dualPlayerData.connectionId;
            var controllerId = dualPlayerData.controllerId;

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