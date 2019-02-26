namespace Turtle
{

    public class MsgType
    {
        const short MsgTypeBase = Julo.TurnBased.MsgType.Highest;

        public const short ServerUpdate = MsgTypeBase + 1;
        public const short ClientUpdate = MsgTypeBase + 2;

        public const short Highest = ClientUpdate;
    }

} // namespace JuloTurtle