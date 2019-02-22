using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OnlinePlayer : NetworkBehaviour, DNMPlayer
    {
        bool ClientStarted = false;
        bool StartCalled = false;

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
            else if(StartCalled)
            {
                Log.Warn("Start was called but ClientStarted=false");
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

        public void SetRole(int role)
        {
            // this change won't have immediate effect because it's a SyncVar (will trigger OnRoleChangedHook() instead)    
            this.role = role;
        }

        public int GetRole()
        {
            return role;
        }

        public void SetUser(UserProfile user)
        {
            this.username = user.GetName();
        }

        public string GetName()
        {
            return username;
        }

        public void Start()
        {
            StartCalled = true;
            Log.Debug("Start: {0} in {1}", isLocalPlayer ? "LOCAL" : "REMOTE", NetworkServer.active ? "host" : "remote client");

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
                l.Init(username, role, DualNetworkManager.GameState.Lobby /* TODO */, Mode.OnlineMode, isLocalPlayer);
            }
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
        /*
        public void Init(string username, int role, DualNetworkManager.GameState gameState, Mode mode, bool isLocal = true)
        {
            foreach (DNMPlayerListener l in listeners)
            {
                l.Init(username, role, gameState, mode, isLocal);
            }
        }

        public void OnReadyChanged(bool isReady)
        {
            foreach(DNMPlayerListener l in listeners)
            {
                l.OnReadyChanged(isReady);
            }
        }

        public void OnRoleChanged(int newRole)
        {
            foreach (DNMPlayerListener l in listeners)
            {
                l.OnRoleChanged(newRole);
            }
        }
        */

    } // class NetworkPlayer

} // namespace Julo.Network
