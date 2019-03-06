using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    public class DualPlayer : MonoBehaviour, IPlayer
    {
        int connectionId = -1;
        short controllerId = -1;
        bool isLocal;

        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();

        public void Start()
        {
            if(transform.parent != DualNetworkManager.instance.playerContainer)
            {
                transform.SetParent(DualNetworkManager.instance.playerContainer);
            }
            else
            {
                Log.Debug("Already on right parent");
            }
        }
        
        public void Init(Mode mode, int connectionId, short playerControllerId, bool isLocal)
        {
            this.connectionId = connectionId;
            this.controllerId = playerControllerId;
            this.isLocal = isLocal;
            
            foreach(var l in listeners)
            {
                l.InitDualPlayer(mode, mode == Mode.OfflineMode || NetworkServer.active, isLocal);
            }
        }
        
        public int ConnectionId()
        {
            return connectionId;
        }

        public short ControllerId()
        {
            return controllerId;
        }

        public bool IsLocal()
        {
            return isLocal;
        }

        /////////////// Listening ///////////////
        
        public void AddDualListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class DualPlayer

} // namespace Julo.Network
