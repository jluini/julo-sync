using System;
using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;
using Julo.Game;

namespace Julo.TurnBased
{
    
    public class TBPlayer : GamePlayer
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
        
    } // class TBPlayer

} // namespace Julo.TurnBased

