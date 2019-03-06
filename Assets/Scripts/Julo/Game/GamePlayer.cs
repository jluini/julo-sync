using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{

    public class GamePlayer : DualPlayer
    {
        public int role = -1;
        public bool isReady = false;
        public string username = "-";

        List<IGamePlayerListener> listeners = new List<IGamePlayerListener>();
        
        public void Init(int role, bool isReady, string username)
        {
            this.role = role;
            this.isReady = isReady;
            this.username = username;
            
            foreach(var l in listeners)
            {
                l.InitGamePlayer(role, isReady, username);
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

        public void SetReady(bool newReady)
        {
            this.isReady = newReady;
            
            foreach(var l in listeners)
            {
                l.OnReadyChanged(newReady);
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

        public bool IsSpectator()
        {
            return role == DNM.SpecRole;
        }
        
        public void GameStarted()
        {
            foreach(var l in listeners)
            {
                l.OnGameStarted();
            }
        }
        
        //  ///////////////////
        
        public void AddGameListener(IGamePlayerListener listener)
        {
            listeners.Add(listener);
        }

        //  ///////////////////

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

        public void OnNameEntered(string newName)
        {
            if(GameClient.instance != null)
            {
                var changed = GameClient.instance.ChangeName(this, newName);

                if(!changed)
                {
                    foreach(var l in listeners)
                    {
                        l.OnNameRejected();
                    }
                }
            }
            else
            {
                Log.Warn("Changing name but no client");
            }
        }

    } // class GamePlayer

} // namespace Julo.Game