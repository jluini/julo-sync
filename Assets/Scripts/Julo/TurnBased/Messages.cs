using UnityEngine.Networking;

using Julo.Logging;
using Julo.Users;

namespace Julo.TurnBased
{

    public class MsgType
    {
        public const short Sarasa = Julo.Network.MsgType.Highest + 1;

        public const short Highest = Sarasa + 10;
    }


} // namespace Julo.TurnBased