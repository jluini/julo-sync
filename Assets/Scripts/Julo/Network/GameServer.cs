
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Julo.Network
{
    
    public abstract class GameServer : MonoBehaviour
    {

        public abstract void StartServer(Mode mode, int numRoles, List<Player>[] playersPerRole);
        public abstract void StartGame();
        public abstract MessageBase GetStateMessage();

    } // class GameServer

} // namespace Julo.Network