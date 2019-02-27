using System.Collections.Generic;

using UnityEngine;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OfflinePlayer : MonoBehaviour, DNMPlayer
    {

        List<DNMPlayerListener> listeners = new List<DNMPlayerListener>();
        UserProfile user;

        int role;
        uint id;

        public void Init(UserProfile user, uint id, int role)
        {
            SetUser(user);
            this.id = id;
            SetRole(role);
        }

        void Start()
        {
            foreach(DNMPlayerListener l in listeners)
            {
                l.Init(user.GetName(), role, DualNetworkManager.GameState.NoGame /* TODO */, Mode.OfflineMode, true, true);
            }
        }

        public uint GetId()
        {
            return id;
        }

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

        public bool IsLocal()
        {
            return true;
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

        /////////////// Listening ///////////////

        public void AddListener(DNMPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class OfflinePlayer

} // namespace Julo.Network
