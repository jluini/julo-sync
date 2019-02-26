using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    
    public abstract class GameServer : MonoBehaviour
    {
        public static GameServer instance = null;

        protected Mode mode;
        protected int numRoles;
        protected List<Player>[] playersPerRole;

        public void StartServer(Mode mode, int numRoles, List<Player>[] playersPerRole)
        {
            instance = this;

            this.mode = mode;
            this.numRoles = numRoles;
            this.playersPerRole = playersPerRole;

            OnStartServer();
        }

        protected virtual void OnStartServer() { }

        // only online mode
        public virtual void WriteInitialData(List<MessageBase> messages)
        {
            messages.Add(new UnityEngine.Networking.NetworkSystem.IntegerMessage(numRoles));
        }

        public abstract void StartGame();

        ///////// Messaging
        

        protected void SendTo(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendTo(who, msgType, msg);
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...
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
            if(mode == Mode.OfflineMode)
            {
                if(who != 0)
                {
                    Log.Warn("Unexpected 'but' id {0} in offline mode", who);
                }
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAllBut(who, msgType, msg);
            }
        }

        // only online mode
        public virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        ///////// Utils
        
        protected List<Player> GetPlayersForRole(int role)
        {
            return playersPerRole[role - 1];
        }

    } // class GameServer

} // namespace Julo.Network