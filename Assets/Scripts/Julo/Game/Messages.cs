using UnityEngine.Networking;

namespace Julo.Game
{
    public class MsgType
    {
        const short MsgTypeBase = Julo.Network.MsgType.Highest;

        public const short StartGame = MsgTypeBase + 1;
        public const short ReadyToStart = MsgTypeBase + 2;

        public const short Highest = ReadyToStart;

    } // class MsgType

    public class GameStatusMessage : MessageBase
    {
        public GameState state;
        public int numRoles;
        public string sceneName;

        public GameStatusMessage()
        {
        }

        public GameStatusMessage(GameState state, int numRoles, string sceneName)
        {
            this.state = state;
            this.numRoles = numRoles;
            this.sceneName = sceneName;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((int)state);
            writer.Write(numRoles);
            writer.Write(sceneName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            state = (GameState)reader.ReadInt32();
            numRoles = reader.ReadInt32();
            sceneName = reader.ReadString();
        }

    } // class GameStatusMessage

    public class StartGameMessage : MessageBase
    {
        public int numRoles;
        public string sceneName;

        public StartGameMessage()
        {
        }

        public StartGameMessage(int numRoles, string sceneName)
        {
            this.numRoles = numRoles;
            this.sceneName = sceneName;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(numRoles);
            writer.Write(sceneName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            numRoles = reader.ReadInt32();
            sceneName = reader.ReadString();
        }

    } // class StartGameMessage

} // namespace Julo.Game