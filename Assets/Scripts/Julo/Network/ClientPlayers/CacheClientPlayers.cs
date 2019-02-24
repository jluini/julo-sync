using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{ 
    public class CacheClientPlayers<T> : ClientPlayers<T>
    {
        Dictionary<uint, T> players;

        public CacheClientPlayers()
        {
            players = new Dictionary<uint, T>();
        }

        public T GetPlayerByNetId(uint netId)
        {
            if(netId == 0)
            {
                Log.Error("Invalid netId=0");
                return default;
            }

            if(players.ContainsKey(netId))
            {
                return players[netId];
            }

            var obj = ClientScene.FindLocalObject(new NetworkInstanceId(netId));
            if(obj == null)
            {
                Log.Error("Object not found");
                return default;
            }

            var ret = obj.GetComponent<T>();
            if(ret == null)
            {
                Log.Error("Component {0} not found", typeof(T));
                return default;
            }

            players[netId] = ret;

            return ret;
        }

    } // class CacheClientPlayers

} // namespace Julo.Network
