using System.Collections.Generic;

using UnityEngine;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{
    public class OfflineDualPlayer : MonoBehaviour, IDualPlayer
    {

        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();

        // this is
        int controllerId;

        public void Init(int controllerId)
        {

            if(controllerId < 0 || controllerId > short.MaxValue)
            {
                var warnMessage = System.String.Format("Invalid controllerId/fake netId = {0}", controllerId);
                Log.Warn(warnMessage);
            }

            this.controllerId = controllerId;
        }

        void Start()
        {
            foreach(IDualPlayerListener l in listeners)
            {
                l.InitDualPlayer(Mode.OfflineMode, true, true);
            }
        }

        public uint NetworkId()
        {
            return (uint)controllerId;
        }

        public int ConnectionId()
        {
            return DNM.LocalConnectionId;
        }

        public short ControllerId()
        {
            return (short)controllerId;
        }

        public bool IsLocal()
        {
            return true;
        }
        
        /////////////// Listening ///////////////

        public void AddListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class OfflineDualPlayer

} // namespace Julo.Network
