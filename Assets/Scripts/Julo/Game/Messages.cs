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

        public const short GameWillStart = MsgTypeBase + 20;
        public const short GameCanceled = MsgTypeBase + 21;
        public const short PrepareToStart = MsgTypeBase + 30;
        public const short StartGame = MsgTypeBase + 31;
        public const short ReadyToStart = MsgTypeBase + 40;

        public const short Highest = ReadyToStart;

    } // class MsgType

    ////////////

    public class ChangeRoleMessage : MessageBase
    {
        public int connectionId;
        public short controllerId;
        public int newRole;

        public ChangeRoleMessage()
        {
        }

        public ChangeRoleMessage(int connectionId, short controllerId, int newRole)
        {
            this.connectionId = connectionId;
            this.controllerId = controllerId;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(connectionId);
            writer.Write(controllerId);
            writer.Write(newRole);
        }

        public override void Deserialize(NetworkReader reader)
        {
            connectionId = reader.ReadInt32();
            controllerId = reader.ReadInt16();
            newRole = reader.ReadInt32();
        }

    } // class ChangeRoleMessage

    public class ChangeUsernameMessage : MessageBase
    {
        public int connectionId;
        public short controllerId;
        public string newName;

        public ChangeUsernameMessage()
        {
        }

        public ChangeUsernameMessage(int connectionId, short controllerId, string newName)
        {
            this.connectionId = connectionId;
            this.controllerId = controllerId;
            this.newName = newName;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(connectionId);
            writer.Write(controllerId);
            writer.Write(newName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            connectionId = reader.ReadInt32();
            controllerId = reader.ReadInt16();
            newName = reader.ReadString();
        }

    } // class ChangeUsernameMessage

    public class ChangeReadyMessage : MessageBase
    {
        public int connectionId;
        public bool newReady;

        public ChangeReadyMessage()
        {
        }

        public ChangeReadyMessage(int connectionId, bool newReady)
        {
            this.connectionId = connectionId;
            this.newReady = newReady;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(connectionId);
            writer.Write(newReady);
        }

        public override void Deserialize(NetworkReader reader)
        {
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

    public class PrepareToStartMessage : MessageBase
    {
        public int numRoles;
        public string sceneName;

        public PrepareToStartMessage()
        {
        }

        public PrepareToStartMessage(int numRoles, string sceneName)
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

    } // class PrepareToStartMessage

} // namespace Julo.Game