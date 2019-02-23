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

        public void SetUser(UserProfile user)
        {
            this.user = user;
        }

        public uint GetId()
        {
            throw new System.NotImplementedException();
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

        public bool IsReady()
        {
            return true;
        }

        public bool IsSpectator()
        {
            return false;
        }

        public int GetRole()
        {
            Log.Error("To be implemented");
            return 0;
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
            foreach (DNMPlayerListener l in listeners)
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

    } // class OfflinePlayer

} // namespace Julo.Network
