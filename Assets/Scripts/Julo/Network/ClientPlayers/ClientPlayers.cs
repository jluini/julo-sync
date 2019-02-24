using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{ 
    public interface ClientPlayers<T>
    {
        T GetPlayerByNetId(uint netId);

    } // interface ClientPlayers

} // namespace Julo.Network
