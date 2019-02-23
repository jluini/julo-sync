using System.Collections.Generic;

using UnityEngine;

using Julo.Network;
using Julo.Logging;

namespace Julo.TurnBased
{
    public class TBPlayer : MonoBehaviour, Player
    {
        List<TBPlayerListener> listeners = new List<TBPlayerListener>();

        DNMPlayer _dnmPlayer;
        DNMPlayer dnmPlayer
        {
            get
            {
                if(_dnmPlayer == null)
                {
                    _dnmPlayer = GetComponent<DNMPlayer>();

                    if(_dnmPlayer == null)
                    {
                        Log.Error("Component DNMPlayer not found!");
                    }
                }

                return _dnmPlayer;
            }
        }

        public uint GetId()
        {
            return dnmPlayer.GetId();
        }
        public string GetName()
        {
            return dnmPlayer.GetName();
        }
        public int GetRole()
        {
            return dnmPlayer.GetRole();
        }
        public bool IsLocal()
        {
            return dnmPlayer.IsLocal();
        }

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

