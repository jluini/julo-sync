using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    
    public abstract class GameClient : MonoBehaviour
    {
        public static GameClient instance = null;

        protected Mode mode;
        protected bool isHosted;
        protected int numRoles;

        ClientPlayers<DNMPlayer> clientPlayers;

        // only local
        GameServer gameServer;

        // local client
        public void StartClient(GameServer server, Mode mode, int numRoles)
        {
            instance = this;

            this.mode = mode;
            this.isHosted = true;
            this.numRoles = numRoles;

            this.gameServer = server; // TODO needed?

            OnStartLocalClient(server);
        }
        
        // remote client
        public void StartClient(StartGameMessage initialMessages)
        {
            instance = this;

            this.mode = Mode.OnlineMode;
            this.isHosted = false;

            var intMsg = initialMessages.ReadInitialMessage<UnityEngine.Networking.NetworkSystem.IntegerMessage>();

            this.numRoles = intMsg.value;
            Log.Debug("Starting with {0} roles", this.numRoles);

            OnStartRemoteClient(initialMessages);
        }

        public virtual void OnStartLocalClient(GameServer gameServer) { }
        public virtual void OnStartRemoteClient(StartGameMessage initialMessages) { }

        protected void SendToServer(short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                GameServer.instance.OnMessage(new WrappedMessage(msgType, msg), 0);
            }
            else
            {
                DualNetworkManager.instance.GameClientSendToServer(msgType, msg);
            }
        }

        // TODO tratar de no recibirlo wrapped
        public virtual void OnMessage(WrappedMessage message)
        {
            throw new System.Exception("Unhandled message");
        }

    } // class GameServer

} // namespace Julo.Network