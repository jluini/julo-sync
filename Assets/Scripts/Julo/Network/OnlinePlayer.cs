using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OnlinePlayer : NetworkBehaviour, DNMPlayer
    {
        bool ClientStarted = false;

        List<DNMPlayerListener> listeners = new List<DNMPlayerListener>();

        [SyncVar]
        string username = "";

        [SyncVar(hook="OnReadyChangedHook")]
        bool ready = false;
        void OnReadyChangedHook(bool newReady)
        {
            //Log.Debug("OnReadyChangedHook({0} -> {1})", ready, newReady);

            this.ready = newReady;

            // no need to redraw when role is initially set, cause it will be redrawn on Start()
            if(this.ClientStarted)
            {
                foreach(DNMPlayerListener l in listeners)
                {
                    l.OnReadyChanged(newReady);
                }
            }
        }

        [SyncVar(hook="OnRoleChangedHook")]
        int role = -1;
        void OnRoleChangedHook(int newRole)
        {
            this.role = newRole;
            
            // no need to redraw when role is initially set, cause it will be redrawn on Start()
            if (this.ClientStarted)
            {
                foreach(DNMPlayerListener l in listeners)
                {
                    l.OnRoleChanged(newRole);
                }
            }
        }
        public void OnClickChangeRole()
        {
            if(!NetworkServer.active)
            {
                Log.Debug("Only server can change role");
                return;
            }

            DualNetworkManager.instance.ChangeRole(this);
        }

        public void Start()
        {
            if(transform.parent != DualNetworkManager.instance.playerContainer)
            {
                transform.SetParent(DualNetworkManager.instance.playerContainer);
            }
            else
            {
                Log.Debug("Already on right parent");
            }

            ClientStarted = true;

            foreach(DNMPlayerListener l in listeners)
            {
                l.Init(username, role, DualNetworkManager.GameState.NoGame /* TODO */, Mode.OnlineMode, NetworkServer.active, isLocalPlayer);
            }
        }


        public uint GetId()
        {
            return netId.Value;
        }

        public string GetName()
        {
            return username;
        }

        public int GetRole()
        {
            return role;
        }
        public void SetRole(int role)
        {
            // this change won't have immediate effect because it's a SyncVar (will trigger OnRoleChangedHook() instead)    
            this.role = role;
        }

        public bool IsLocal()
        {
            return isLocalPlayer;
        }

        public void SetUser(UserProfile user)
        {
            this.username = user.GetName();
        }

        public bool IsReady()
        {
            return ready;
        }

        public bool IsSpectator()
        {
            return role < DNM.FirstPlayerRole;
        }

        public void SetReady(bool readyState)
        {
            // this change won't have immediate effect because it's a SyncVar (will trigger OnReadyChangedHook() instead)    
            this.ready = readyState;
        }

        /////////////// Listening ///////////////

        public void AddListener(DNMPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class NetworkPlayer

} // namespace Julo.Network
