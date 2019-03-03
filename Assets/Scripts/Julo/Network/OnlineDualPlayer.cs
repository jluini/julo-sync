using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Users;
using Julo.Logging;
using Julo.Game; // TODO remove!

namespace Julo.Network
{
    public class OnlineDualPlayer : NetworkBehaviour, IDualPlayer
    {
        //bool ClientStarted = false;

        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();

        public int connectionId = -1;
        public short controllerId = -1;

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

            Log.Debug("START: {0}:{1}", connectionId, controllerId);

            DualClient.instance.StartOnlinePlayer(this);
        }
        
        public void Init(int connectionId, short playerControllerId)
        {
            Log.Debug("Initting {0}:{1}", connectionId, playerControllerId);

            this.connectionId = connectionId;
            this.controllerId = playerControllerId;
        }
        
        public uint NetworkId()
        {
            return netId.Value;
        }

        public int ConnectionId()
        {
            return connectionId;
        }

        public short ControllerId()
        {
            return controllerId;
        }

        public bool IsLocal()
        {
            return isLocalPlayer;
        }


        /* this is game level
        
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
                foreach(IDualPlayerListener l in listeners)
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
                foreach(IDualPlayerListener l in listeners)
                {
                    l.OnRoleChanged(newRole);
                }
            }
        }
        */

        /*
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
        */
        /*
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
        */
        /////////////// Listening ///////////////

        public void AddListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class OnlineDualPlayer

} // namespace Julo.Network
