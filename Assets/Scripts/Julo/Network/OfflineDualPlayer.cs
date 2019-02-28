using System.Collections.Generic;

using UnityEngine;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OfflineDualPlayer : MonoBehaviour, IDualPlayer
    {

        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();
        UserProfile user;

        int role;
        uint id;

        public void Init(UserProfile user, uint id, int role)
        {
            //SetUser(user);
            this.id = id;
            //SetRole(role);
        }

        void Start()
        {
            foreach(IDualPlayerListener l in listeners)
            {
                l.Init(/*user.GetName(), role, DualNetworkManager.GameState.NoGame /* TODO * /, */Mode.OfflineMode, true, true);
            }
        }

        public uint GetId()
        {
            return id;
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
