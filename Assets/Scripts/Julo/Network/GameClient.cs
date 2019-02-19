using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network
{
    
    public abstract class GameClient : MonoBehaviour
    {

        public abstract void StartClient(Mode mode, bool isHosted, int numRoles);
        public abstract void StartGame(NetworkReader messageReader);
        public abstract void LateJoinGame(NetworkReader messageReader);

    } // class GameServer

} // namespace Julo.Network