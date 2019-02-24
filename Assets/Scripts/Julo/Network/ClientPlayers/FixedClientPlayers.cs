using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    public class FixedClientPlayers<T> : ClientPlayers<T>
    {
        Dictionary<uint, T> players;

        public FixedClientPlayers(Dictionary<uint, T> players)
        {
            this.players = players;
        }

        public T GetPlayerByNetId(uint netId)
        {
            if(netId == 0 || !players.ContainsKey(netId))
            {
                Log.Error("Invalid or missing player={0}", netId);

                return default;
            }

            return players[netId];
        }

    } // class FixedClientPlayers

} // namespace Julo.Network
