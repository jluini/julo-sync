using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network
{
    
    public abstract class GameClient : MonoBehaviour
    {

        public abstract void StartClient(Mode mode, bool isHosted, int numRoles);
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