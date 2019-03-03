using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{

    [RequireComponent(typeof(IDualPlayer))]
    public class GamePlayer : MonoBehaviour
    {
        List<IGamePlayerListener> listeners = new List<IGamePlayerListener>();

        public int role = -1;
        public string username = "-";

        public void OnClickChangeRole()
        {
            if(GameServer.instance != null)
            {
                GameServer.instance.ChangeRole(this);
            }
            else
            {
                Log.Warn("Clicking change role but no server");
            }
        }

        public void SetRole(int newRole)
        {
            this.role = newRole;
            foreach(var l in listeners)
            {
                l.OnRoleChanged(newRole);
            }
        }

        public void SetUsername(string username)
        {
            this.username = username;
            foreach(var l in listeners)
            {
                l.OnNameChanged(username);
            }
        }

        public void AddListener(IGamePlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class GamePlayer

} // namespace Julo.Game