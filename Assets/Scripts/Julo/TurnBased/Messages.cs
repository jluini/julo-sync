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

        public const short StartTurn = MsgTypeBase + 3;
        public const short EndTurn = MsgTypeBase + 4;


        public const short Highest = EndTurn;
    }

    public class TurnMessage : MessageBase
    {
        public uint playerNetId;

        public TurnMessage()
        {
        }

        public TurnMessage(uint playerNetId)
        {
            this.playerNetId = playerNetId;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerNetId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerNetId = reader.ReadUInt32();
        }
    }

} // namespace Julo.TurnBased