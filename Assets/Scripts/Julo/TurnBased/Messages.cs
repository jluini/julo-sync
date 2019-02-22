using UnityEngine.Networking;

using Julo.Logging;
using Julo.Users;

namespace Julo.TurnBased
{

    public class MsgType
    {
        public const short GameState = Julo.Network.MsgType.Highest + 1;

        public const short Highest = GameState;
    }


} // namespace Julo.TurnBased