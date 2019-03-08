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
        
        public void Init(Mode mode, bool isLocal, int connectionId, short controllerId)
        {
            this.connectionId = connectionId;
            this.controllerId = controllerId;
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

        /////////////// Remove player ///////////////
        
        
        public void RemovePlayerCommand()
        {

            if(DualClient.instance != null)
            {
                DualClient.instance.RemovePlayerCommand(this);
            }
            else
            {
                Log.Warn("RemovePlayer but no client");
            }
        }

        /////////////// Listening ///////////////

        public void AddDualListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class DualPlayer

} // namespace Julo.Network
