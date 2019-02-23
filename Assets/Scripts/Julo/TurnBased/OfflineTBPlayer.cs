using System.Collections.Generic;

using UnityEngine;

using Julo.Network;
using Julo.Logging;

namespace Julo.TurnBased
{
    [RequireComponent(typeof(OfflinePlayer))]
    public class OfflineTBPlayer : MonoBehaviour, TBPlayer
    {
        List<TBPlayerListener> listeners = new List<TBPlayerListener>();

        OfflinePlayer _player;
        OfflinePlayer player
        {
            get
            {
                if(_player == null)
                {
                    _player = GetComponent<OfflinePlayer>();

                    if(_player == null)
                    {
                        Log.Error("Component OfflinePlayer not found!");
                    }
                }

                return _player;
            }
        }
        
        public void TurnIsStartedRpc()
        {
            throw new System.NotImplementedException();
        }
        
        public void TurnIsOverCommand()
        {
            throw new System.NotImplementedException();
        }

        public void GameStateCommand()
        {
            throw new System.NotImplementedException();
        }

        public string GetName()
        {
            if(player == null)
            {
                Log.Error("Invalid call of GetName");
                return "-";
            }

            return player.GetName();
        }

        public int GetRole()
        {
            if(player == null)
            {
                Log.Error("Invalid call of GetRole");
                return 0;
            }

            return player.GetRole();
        }

        /////////////// Listening ///////////////

        public void AddListener(TBPlayerListener listener)
        {
            listeners.Add(listener);
        }

        public void SetPlaying(bool isPlaying)
        {
            foreach (TBPlayerListener l in listeners)
            {
                l.SetPlaying(isPlaying);
            }
        }

    } // class OfflineTBPlayer

} // namespace Julo.TurnBased;

