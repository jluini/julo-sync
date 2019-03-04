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

    public class PlayerMessage : MessageBase
    {
        public uint playerId;

        public PlayerMessage()
        {
        }

        public PlayerMessage(GamePlayer player)
        {
            playerId = player == null ? 0 : player.PlayerId();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerId = reader.ReadUInt32();
        }

    } // class PlayerMessage


} // namespace Julo.TurnBased
