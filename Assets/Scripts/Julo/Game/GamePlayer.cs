using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{

    [RequireComponent(typeof(IDualPlayer))]
    public class GamePlayer : MonoBehaviour, IPlayer
    {
        public int role = -1;
        public bool isReady = false;
        public string username = "-";

        List<IGamePlayerListener> listeners = new List<IGamePlayerListener>();

        IDualPlayer _dualPlayer;
        IDualPlayer dualPlayer
        {
            get
            {
                if(_dualPlayer == null)
                {
                    _dualPlayer = GetComponent<IDualPlayer>();

                    if(_dualPlayer == null)
                    {
                        Log.Error("Component IDualPlayer not found!");
                    }
                }

                return _dualPlayer;
            }
        }

        public uint PlayerId()
        {
            return dualPlayer.PlayerId();
        }

        public bool IsLocal()
        {
            return dualPlayer.IsLocal();
        }

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

        public void Init(GameState gameState, int newRole, bool isReady, string username)
        {
            this.role = newRole;
            this.isReady = isReady;
            this.username = username;

            foreach(var l in listeners)
            {
                l.InitGamePlayer(gameState, role, isReady, username);
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

        public void AddListener(IGamePlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class GamePlayer

} // namespace Julo.Game