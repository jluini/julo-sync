using System.Collections.Generic;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;

namespace Julo.Network
{
    public class DualClient
    {
        public static DualClient instance = null;

        bool isInitialized = false;

        protected Mode mode;
        protected bool isHosted;

        protected DualServer server;

        // creates hosted client
        public DualClient(Mode mode, DualServer server)
        {
            instance = this;

            this.mode = mode;
            this.server = server;
            isHosted = true;
        }

        // creates remote client
        public DualClient()
        {
            instance = this;

            this.mode = Mode.OnlineMode;
            server = null;
            isHosted = false;

            isInitialized = false;
        }

        // only remote
        public virtual void InitializeState(MessageStackMessage messageStack)
        {
            Log.Debug("INIT STATE");

            var numPlayersMessage = messageStack.ReadMessage<IntegerMessage>();
            var numPlayers = numPlayersMessage.value;

            //Log.Debug("Detectados {0} players preexistentes", numPlayers);

            for(int i = 0; i < numPlayers; i++)
            {

                var dualPlayerMsg = messageStack.ReadMessage<DualPlayerMessage>();

                var netId = dualPlayerMsg.netId;

                Log.Debug("            {0} = {1}/{2}", netId, dualPlayerMsg.connectionId, dualPlayerMsg.controllerId);
                

                if(pendingPlayers.ContainsKey(netId))
                {
                    var player = pendingPlayers[netId];
                    pendingPlayers.Remove(netId);

                    ResolvePlayer(player, dualPlayerMsg, messageStack);
                }
                else
                {
                    pendingMessages.Add(netId, new PendingPlayerStack(dualPlayerMsg, messageStack));
                }
            }
        }

        // only remote
        public virtual void OnPlayerResolved(OnlineDualPlayer player, MessageStackMessage messageStack)
        {
            // noop
        }

        Dictionary<uint, PendingPlayerStack> pendingMessages = new Dictionary<uint, PendingPlayerStack>();
        Dictionary<uint, OnlineDualPlayer> pendingPlayers  = new Dictionary<uint, OnlineDualPlayer>();

        //
        public void OnNewPlayerMessage(MessageStackMessage messageStack)
        {

            if(!isHosted)
            {
                var dualPlayerMessage = messageStack.ReadMessage<DualPlayerMessage>();
                var netId = dualPlayerMessage.netId;

                Log.Debug("NEW PLAYER id={0}", netId);

                // Log.Debug("Received message netId={0}, connId={1}, controller={2}", netId, dualPlayerMessage.connectionId, dualPlayerMessage.controllerId);

                bool isPendingPlayer = pendingPlayers.ContainsKey(netId);

                if(isPendingPlayer)
                {
                    ResolvePlayer(pendingPlayers[netId], dualPlayerMessage, messageStack);
                    pendingPlayers.Remove(netId);
                }
                else
                {
                    pendingMessages.Add(netId, new PendingPlayerStack(dualPlayerMessage, messageStack));
                }
            }
        }


        public void StartOnlinePlayer(OnlineDualPlayer player)
        {
            if(!isHosted)
            {
                uint netId = player.netId.Value;

                Log.Debug("START PLAYER id={0}", netId);

                bool isPendingMessage = pendingMessages.ContainsKey(netId);

                if(isPendingMessage)
                {
                    ResolvePlayer(player, pendingMessages[netId].dualPlayerData, pendingMessages[netId].stack);
                    pendingMessages.Remove(netId);
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

        void ResolvePlayer(OnlineDualPlayer player, DualPlayerMessage dualPlayer, MessageStackMessage messageStack)
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

            OnPlayerResolved(player, messageStack);
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

} // namespace Julo.Network