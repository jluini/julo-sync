

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    
    public abstract class GameServer : MonoBehaviour
    {
        protected Mode mode;
        protected int numRoles;
        protected List<Player>[] playersPerRole;

        public void StartServer(Mode mode, int numRoles, List<Player>[] playersPerRole)
        {
            this.mode = mode;
            this.numRoles = numRoles;
            this.playersPerRole = playersPerRole;

            OnStartServer();
        }

        protected abstract void OnStartServer();

        protected List<Player> GetPlayersForRole(int role)
        {
            return playersPerRole[role - 1];
        }

        public abstract void StartGame();

        protected void SendTo(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendTo(who, msgType, msg);
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            // TODO tratar de no recibirlo wrapped

            if(mode == Mode.OfflineMode)
            {
                GameClient.instance.OnMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
            }
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