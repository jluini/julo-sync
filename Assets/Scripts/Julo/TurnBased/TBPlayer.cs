using System;
using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;
using Julo.Game;

namespace Julo.TurnBased
{
    
    // TODO should implement IDualPlayer?
    [RequireComponent(typeof(GamePlayer))]
    public class TBPlayer : MonoBehaviour, IPlayer
    {
        // only in server
        public DateTime lastUse;

        List<ITurnBasedPlayerListener> listeners = new List<ITurnBasedPlayerListener>();

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

        GamePlayer _gamePlayer;
        GamePlayer gamePlayer
        {
            get
            {
                if(_gamePlayer == null)
                {
                    _gamePlayer = GetComponent<GamePlayer>();

                    if(_gamePlayer == null)
                    {
                        Log.Error("Component GamePlayer not found!");
                    }
                }

                return _gamePlayer;
            }
        }

        public int GetRole()
        {
            return gamePlayer.role;
        }

        public void AddListener(ITurnBasedPlayerListener listener)
        {
            listeners.Add(listener);
        }

        public void SetPlaying(bool isPlaying)
        {
            foreach(var l in listeners)
            {
                l.SetPlaying(isPlaying);
            }
        }

        public uint PlayerId()
        {
            return dualPlayer.PlayerId();
        }

        public int ConnectionId()
        {
            return dualPlayer.ConnectionId();
        }

        public short ControllerId()
        {
            return dualPlayer.ControllerId();
        }

        public bool IsLocal()
        {
            return dualPlayer.IsLocal();
        }

    } // class TBPlayer

} // namespace Julo.TurnBased

