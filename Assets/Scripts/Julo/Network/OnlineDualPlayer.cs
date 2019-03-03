using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Users;
using Julo.Logging;
using Julo.Game; // TODO remove!

namespace Julo.Network
{
    public class OnlineDualPlayer : NetworkBehaviour, IDualPlayer
    {
        List<IDualPlayerListener> listeners = new List<IDualPlayerListener>();

        public int connectionId = -1;
        public short controllerId = -1;

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

            //Log.Debug("START: {0}:{1}", connectionId, controllerId);

            DualClient.instance.StartOnlinePlayer(this);
        }
        
        public void Init(int connectionId, short playerControllerId)
        {
            this.connectionId = connectionId;
            this.controllerId = playerControllerId;
        }
        
        public uint NetworkId()
        {
            return netId.Value;
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
            return isLocalPlayer;
        }

        /////////////// Listening ///////////////

        public void AddListener(IDualPlayerListener listener)
        {
            listeners.Add(listener);
        }

    } // class OnlineDualPlayer

} // namespace Julo.Network
