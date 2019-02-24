using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network
{
    
    public abstract class GameClient : MonoBehaviour
    {
        //public static GameClient instance = null;

        protected Mode mode;
        protected bool isHosted;
        protected int numRoles;

        //protected Dictionary<uint, Player> clientPlayers;
        ClientPlayers<DNMPlayer> clientPlayers;

        public void StartClient(Mode mode, bool isHosted, int numRoles)
        {
            //instance = this;

            this.mode = mode;
            this.isHosted = isHosted;
            this.numRoles = numRoles;

            clientPlayers = new CacheClientPlayers<DNMPlayer>();

            OnStartClient();
        }

        public abstract void OnStartClient();

        public abstract void StartGame(NetworkReader messageReader);
        public abstract void LateJoinGame(NetworkReader messageReader);

        protected void SendToServer(short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameClientSendToServer(msgType, msg);
        }

        public virtual void OnMessage(WrappedMessage message)
        {
            throw new System.Exception("Unhandled message");
        }

    } // class GameServer

} // namespace Julo.Network