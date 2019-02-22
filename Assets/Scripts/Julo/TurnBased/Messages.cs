using UnityEngine.Networking;

using Julo.Logging;
using Julo.Users;

namespace Julo.TurnBased
{

    public class MsgType
    {
        const short MsgTypeBase = Julo.Network.MsgType.Highest;
        public const short InitialState = MsgTypeBase + 1;
        public const short GameState = MsgTypeBase + 2;


        public const short Highest = GameState;
    }


} // namespace Julo.TurnBased