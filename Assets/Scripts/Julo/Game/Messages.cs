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

        // Players

        public const short PlayerDisconnected = MsgTypeBase + 41;
        public const short PlayerResigns = MsgTypeBase + 42;


        public const short Highest = PlayerResigns;

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
    
    public class GamePlayerSnapshot : MessageBase
    {
        public GamePlayerState playerState;
        public int role;
        public bool isReady;
        public string username;

        public GamePlayerSnapshot()
        {
        }

        public GamePlayerSnapshot(GamePlayer gamePlayer)
        {
            this.playerState = gamePlayer.playerState;
            this.role = gamePlayer.role;
            this.isReady = gamePlayer.isReady;
            this.username = gamePlayer.username;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((int)playerState);
            writer.Write(role);
            writer.Write(isReady);
            writer.Write(username);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerState = (GamePlayerState)reader.ReadInt32();
            role = reader.ReadInt32();
            isReady = reader.ReadBoolean();
            username = reader.ReadString();
        }

    } // class GamePlayerSnapshot

    public class GameContextSnapshot : MessageBase
    {
        public GameState gameState;
        public int numRoles;
        public string sceneName;

        public GameContextSnapshot()
        {
        }

        public GameContextSnapshot(GameState gameState, int numRoles, string sceneName)
        {
            this.gameState = gameState;
            this.numRoles = numRoles;
            this.sceneName = sceneName;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((int)gameState);
            writer.Write(numRoles);
            writer.Write(sceneName);
        }

        public override void Deserialize(NetworkReader reader)
        {
            gameState = (GameState)reader.ReadInt32();
            numRoles = reader.ReadInt32();
            sceneName = reader.ReadString();
        }

    } // class GameContextSnapshot

    public class PrepareToStartMessage : MessageBase
    {
        // TODO is this data needed? it should be already synchronized
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