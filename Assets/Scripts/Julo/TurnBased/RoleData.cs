using System.Collections.Generic;

using Julo.Network;

namespace Julo.TurnBased
{
    // only in server
    public class RoleData
    {
        public bool isAlive;

        public RoleData()
        {
            this.isAlive = true;
        }

    } // class RoleData

} // namespace Julo.TurnBased