﻿using System.Collections.Generic;

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

        public uint PlayerId()
        {
            return dualPlayer.PlayerId();
        }

        public bool IsLocal()
        {
            return dualPlayer.IsLocal();
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

    } // class TBPlayer

} // namespace Julo.TurnBased

