using UnityEngine.Networking;

using Julo.Game;

namespace Julo.TurnBased
{
    public class MsgType
    {
        const short MsgTypeBase = Julo.Game.MsgType.Highest;

        public const short StartTurn = MsgTypeBase + 1;
        public const short EndTurn = MsgTypeBase + 2;

        public const short Highest = EndTurn;
    }

} // namespace Julo.TurnBased
