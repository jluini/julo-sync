using System.Collections.Generic;

using UnityEngine;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OfflineDualPlayer : MonoBehaviour, IDualPlayer
    {

        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();

        // this is
        int controllerId;

        public void Init(int controllerId)
        {

            if(controllerId < 0 || controllerId > short.MaxValue)
            {
                var warnMessage = System.String.Format("Invalid controllerId/fake netId = {0}", controllerId);
                Log.Warn(warnMessage);
                //throw new System.Exception(warnMessage);
            }

            this.controllerId = controllerId;
        }

        void Start()
        {
            foreach(IDualPlayerListener l in listeners)
            {
                l.InitDualPlayer(/*user.GetName(), role, DualNetworkManager.GameState.NoGame /* TODO * /, */Mode.OfflineMode, true, true);
            }
        }

        public uint NetworkId()
        {
            return (uint)controllerId;
        }

        public int ConnectionId()
        {
            return DNM.LocalConnectionId;
        }

        public short ControllerId()
        {
            return (short)controllerId;
        }

        public bool IsLocal()
        {
            return true;
        }
        /*
        public string GetName()
        {
            if(user != null)
            {
                return user.GetName();
            }
            else
            {
                return "-";
            }
        }

        public int GetRole()
        {
            return role;
        }

        public void SetRole(int newRole)
        {
            this.role = newRole;
            foreach(DNMPlayerListener l in listeners)
            {
                l.OnRoleChanged(newRole);
            }
        }

        public void OnClickChangeRole()
        {
            DualNetworkManager.instance.ChangeRole(this);
        }

        public void SetUser(UserProfile user)
        {
            this.user = user;
        }

        public bool IsReady()
        {
            return true;
        }

        public bool IsSpectator()
        {
            return false;
        }
        */
        /////////////// Listening ///////////////

        public void AddListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class OfflineDualPlayer

} // namespace Julo.Network
