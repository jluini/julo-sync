using System.Collections.Generic;

using UnityEngine;
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

        // only hosted
        protected DualServer server;

        // only remote?
        DualPlayer playerModel;

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
        public DualClient(Mode mode, DualServer server, DualPlayer playerModel)
        {
            instance = this;

            this.mode = mode;
            this.server = server;
            this.playerModel = playerModel;

            isHosted = server != null;

            if(!isHosted && mode == Mode.OfflineMode)
            {
                Log.Error("Non-hosted client not allowed in offline mode");
            }
        }

        // only remote
        public virtual void InitializeState(int connectionNumber, MessageStackMessage messageStack)
        {
            clientConnections = new ConnectionsAndPlayers(false, connectionNumber);
            
            var numPlayersMessage = messageStack.ReadMessage<IntegerMessage>();
            var numPlayers = numPlayersMessage.value;

            for(int i = 0; i < numPlayers; i++)
            {
                var playerScreenshot = messageStack.ReadMessage<PlayerMessage>();

                var connId = playerScreenshot.connectionId;
                var controllerId = playerScreenshot.controllerId;

                bool isLocal = connId == connectionNumber;

                if(isLocal)
                {
                    Log.Warn("Already a local client??");
                }

                AddPlayer(connId, controllerId, isLocal, messageStack);
            }
        }

        void AddPlayer(int connId, short controllerId, bool isLocal, MessageStackMessage messageStack)
        {
            var newPlayer = GameObject.Instantiate<DualPlayer>(playerModel);
            newPlayer.Init(mode, connId, controllerId, isLocal);
            clientConnections.AddPlayer(newPlayer);
            OnPlayerAdded(newPlayer, messageStack);
        }

        protected virtual void OnPlayerAdded(DualPlayer player, MessageStackMessage messageStack)
        {
            // noop
        }

        // only remote client
        public void OnNewPlayerMessage(MessageStackMessage messageStack)
        {
            if(!isHosted)
            {
                var playerScreenshot = messageStack.ReadMessage<PlayerMessage>();

                var connId = playerScreenshot.connectionId;
                var controllerId = playerScreenshot.controllerId;

                var isLocal = connId == clientConnections.localConnectionNumber;

                AddPlayer(connId, controllerId, isLocal, messageStack);
            }
        }
        
        // only remote
        public void RemovePlayer(uint playerId)
        {
            if(isHosted)
            {
                Log.Error("I'm hosted");
                return;
            }
            // TODO 
            //clientConnections.RemovePlayer(playerId);
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