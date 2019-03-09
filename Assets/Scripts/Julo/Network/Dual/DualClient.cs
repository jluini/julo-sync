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
        DualContext clientContext;

        public DualContext dualContext
        {
            get
            {
                if(isHosted)
                {
                    return server.dualContext;
                }
                else
                {
                    return clientContext;
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
        public virtual void InitializeState(int connectionNumber, ListOfMessages listOfMessages)
        {
            clientContext = new DualContext(false, connectionNumber);

            var numPlayersMessage = listOfMessages.ReadMessage<IntegerMessage>();
            var numPlayers = numPlayersMessage.value;

            for(int i = 0; i < numPlayers; i++)
            {
                AddPlayer(listOfMessages);
            }
        }

        // only remote client
        public void OnNewPlayerMessage(ListOfMessages listOfMessages)
        {
            if(!isHosted)
            {
                AddPlayer(listOfMessages);
            }
        }
        
        // onyl remote
        void AddPlayer(ListOfMessages listOfMessages)
        {
            var playerSnapshot = listOfMessages.ReadMessage<DualPlayerSnapshot>();

            var connId = playerSnapshot.connectionId;
            var controllerId = playerSnapshot.controllerId;
            var isLocal = connId == clientContext.localConnectionNumber;

            var newPlayer = GameObject.Instantiate<DualPlayer>(playerModel);

            newPlayer.Init(mode, isLocal, connId, controllerId);

            clientContext.AddPlayer(newPlayer);
            OnPlayerAdded(newPlayer, listOfMessages);
        }
        
        // only remote
        protected virtual void OnPlayerAdded(DualPlayer player, ListOfMessages listOfMessages)
        {
            // noop
        }
        
        // only remote
        public void RemovePlayerCommand(DualPlayer playerToRemove)
        {
            if(!playerToRemove.IsLocal())
            {
                Log.Error("Trying to remove a non-local player");
                return;
            }

            SendToServer(MsgType.RemovePlayer, new DualPlayerSnapshot(playerToRemove));
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

        public void StopClient()
        {
            if(!isHosted)
            {
                foreach(var p in clientContext.AllPlayers())
                {
                    GameObject.Destroy(p.gameObject);
                }
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
            switch(message.messageType)
            {
                case MsgType.RemovePlayer:
                    if(isHosted)
                    {
                        return;
                    }

                    var playerMsg = message.ReadInternalMessage<DualPlayerSnapshot>();

                    var connId = playerMsg.connectionId;
                    var controllerId = playerMsg.controllerId;

                    if(!dualContext.RemovePlayer(connId, controllerId))
                    {
                        Log.Error("Could not remove player {0}:{1}", connId, controllerId);
                    }

                    break;

                default:
                    var msg = string.Format("Unhandled message number={0}", message.messageType - MsgType.Highest);
                    throw new System.Exception(msg);
                    //break;
            }
        }

    } // class DualClient

} // namespace Julo.Network