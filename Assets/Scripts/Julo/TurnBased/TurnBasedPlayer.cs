using System;
using System.Collections.Generic;

using Julo.Logging;
using Julo.Game;

namespace Julo.TurnBased
{
    
    public class TurnBasedPlayer : GamePlayer
    {
        // only in server
        public DateTime lastUse;

        bool isPlaying = false;

        List<ITurnBasedPlayerListener> listeners = new List<ITurnBasedPlayerListener>();
        
        public void SetPlaying(bool isPlaying)
        {
            if(isPlaying == this.isPlaying)
            {
                Log.Warn("isPlaying is already {0}", isPlaying);
                return;
            }

            this.isPlaying = isPlaying;

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

