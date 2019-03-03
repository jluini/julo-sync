using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Game
{
    public class MsgType
    {
        const short MsgTypeBase = Julo.Network.MsgType.Highest;

        public const short StartGame = MsgTypeBase + 1;
        public const short ReadyToStart = MsgTypeBase + 2;

        public const short Highest = ReadyToStart;

    } // class MsgType

    ////////////

    public class GamePlayerMessage : MessageBase
    {
        public int role;
        public string username;

        public GamePlayerMessage()
        {
        }

        public GamePlayerMessage(int role, string username)
        {
            this.role = role;
            this.username = username;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(role);
            writer.Write(username);
        }

        public override void Deserialize(NetworkReader reader)
        {
            role = reader.ReadInt32();
            username = reader.ReadString();
        }

    } // class GamePlayerMessage

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
            Log.Debug("Writing {0}:{1}:{2}", (int)state, numRoles, sceneName);
            writer.Write((int)state);
            writer.Write(numRoles);
            writer.Write(sceneName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            var stateInt = reader.ReadInt32();
            state = (GameState)stateInt;
            Log.Debug("Reading {0}", stateInt);
            numRoles = reader.ReadInt32();
            Log.Debug("Reading ---:{0}", numRoles);
            sceneName = reader.ReadString();
            Log.Debug("Reading ---:---:{0}", sceneName);
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