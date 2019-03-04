using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Game
{
    public class MsgType
    {
        const short MsgTypeBase = Julo.Network.MsgType.Highest;

        public const short ChangeRole = MsgTypeBase + 1;
        public const short ChangeReady = MsgTypeBase + 2;
        public const short ChangeUsername = MsgTypeBase + 3;

        public const short StartGame = MsgTypeBase + 20;
        public const short ReadyToStart = MsgTypeBase + 30;

        public const short Highest = ReadyToStart;

    } // class MsgType

    ////////////

    public class ChangeRoleMessage : MessageBase
    {
        public uint playerId;
        public int newRole;

        public ChangeRoleMessage()
        {
        }

        public ChangeRoleMessage(uint playerId, int newRole)
        {
            this.playerId = playerId;
            this.newRole = newRole;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerId);
            writer.Write(newRole);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerId = reader.ReadUInt32();
            newRole = reader.ReadInt32();
        }

    } // class ChangeRoleMessage

    public class ChangeReadyMessage : MessageBase
    {
        //public uint playerId;
        public int connectionId;
        public bool newReady;

        public ChangeReadyMessage()
        {
        }

        public ChangeReadyMessage(/*uint playerId, */int connectionId, bool newReady)
        {
            //this.playerId = playerId;
            this.connectionId = connectionId;
            this.newReady = newReady;
        }

        public override void Serialize(NetworkWriter writer)
        {
            //writer.Write(playerId);
            writer.Write(connectionId);
            writer.Write(newReady);
        }

        public override void Deserialize(NetworkReader reader)
        {
            //playerId = reader.ReadUInt32();
            connectionId = reader.ReadInt32();
            newReady = reader.ReadBoolean();
        }

    } // class ChangeReadyMessage

    public class GamePlayerMessage : MessageBase
    {
        public int role;
        public bool isReady;
        public string username;

        public GamePlayerMessage()
        {
        }

        public GamePlayerMessage(int role, bool isReady, string username)
        {
            this.role = role;
            this.isReady = isReady;
            this.username = username;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(role);
            writer.Write(isReady);
            writer.Write(username);
        }

        public override void Deserialize(NetworkReader reader)
        {
            role = reader.ReadInt32();
            isReady = reader.ReadBoolean();
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
            writer.Write((int)state);
            writer.Write(numRoles);
            writer.Write(sceneName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            var stateInt = reader.ReadInt32();
            state = (GameState)stateInt;
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