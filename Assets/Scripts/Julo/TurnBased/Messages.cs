using UnityEngine.Networking;

namespace Julo.TurnBased
{
    public class MsgType
    {
        const short MsgTypeBase = Julo.Game.MsgType.Highest;

        public const short StartTurn = MsgTypeBase + 1;
        public const short EndTurn = MsgTypeBase + 2;

        public const short Highest = EndTurn;
    }

    public class TurnMessage : MessageBase
    {
        public uint playerId;

        public TurnMessage()
        {
        }

        public TurnMessage(uint playerId)
        {
            this.playerId = playerId;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerId = reader.ReadUInt32();
        }
    }

} // namespace Julo.TurnBased
