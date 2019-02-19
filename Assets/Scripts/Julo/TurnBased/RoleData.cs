using System.Collections.Generic;

using Julo.Network;

namespace Julo.TurnBased
{
    public class RoleData
    {

        public bool isAlive;

        public List<TBPlayer> players;

        public RoleData(List<TBPlayer> players)
        {
            this.players = players;
            this.isAlive = true;
        }

    } // class RoleData

} // namespace Julo.TurnBased