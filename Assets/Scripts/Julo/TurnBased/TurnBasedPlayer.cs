using System;
using System.Collections.Generic;

using Julo.Game;

namespace Julo.TurnBased
{
    
    public class TurnBasedPlayer : GamePlayer
    {
        // only in server
        public DateTime lastUse;

        List<ITurnBasedPlayerListener> listeners = new List<ITurnBasedPlayerListener>();
        
        public void SetPlaying(bool isPlaying)
        {
            foreach(var l in listeners)
            {
                l.SetPlaying(isPlaying);
            }
        }

        public void AddTBListener(ITurnBasedPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class TurnBasedPlayer

} // namespace Julo.TurnBased

