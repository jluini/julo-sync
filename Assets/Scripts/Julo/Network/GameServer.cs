

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    
    public abstract class GameServer : MonoBehaviour
    {
        public abstract void StartServer(Mode mode, int numRoles, List<Player>[] playersPerRole);
        public abstract void StartGame();

        protected void SendTo(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendTo(who, msgType, msg);
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
        }

        protected void SendToAllBut(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendToAllBut(who, msgType, msg);
        }

        public virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        public abstract MessageBase GetStateMessage();

    } // class GameServer

} // namespace Julo.Network