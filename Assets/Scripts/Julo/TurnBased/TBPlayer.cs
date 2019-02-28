
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
        List<TBPlayerListener> listeners = new List<TBPlayerListener>();

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

        public uint GetId()
        {
            return dualPlayer.GetId();
        }
        public bool IsLocal()
        {
            return dualPlayer.IsLocal();
        }
        /*
        public string GetName()
        {
            return dualPlayer.GetName();
        }
        public int GetRole()
        {
            return dualPlayer.GetRole();
        }
        */

        public void AddListener(TBPlayerListener listener)
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

