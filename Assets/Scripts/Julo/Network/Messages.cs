using UnityEngine.Networking;

using Julo.Logging;
using Julo.Users;

namespace Julo.Network
{

    public class MsgType
    {
        const short MsgTypeBase = UnityEngine.Networking.MsgType.Highest;

        // LOBBY

        // Sent from clients to server to mark themselves as ready/not-ready
        public const short ClientSetReady = MsgTypeBase + 1;

        // JOINING

        // Sent from clients to server to request initial state of the lobby/game
        public const short StatusRequest = MsgTypeBase + 2;

        // Sent from server to clients to inform initial game state
        public const short InitialStatus = MsgTypeBase + 3;

        // STARTING GAME

        // Sent from server to clients
        //public const short GameWillStart = MsgTypeBase + 4;

        // Sent from server to clients to deliver initial game state
        public const short Prepare = MsgTypeBase + 4;

        // Sent from clients to server after Prepare
        public const short ReadyToSpawn = MsgTypeBase + 5;
        
        // Sent from clients to server after receiving all initial unet ObjectSpawn's
        public const short SpawnOk = MsgTypeBase + 6;

        // Sent from server to clients when all sent SpawnOk
        public const short GameStarted = MsgTypeBase + 7;

        public const short Highest = GameStarted;
    }

    /// <summary>
    /// Message sent from clients to change its ready state.
    /// </summary>
    public class ReadyMessage : MessageBase
    {
        public bool value;

        public ReadyMessage()
        {
            // no op
        }

        public ReadyMessage(bool value)
        {
            this.value = value;
        }

        public override void Deserialize(NetworkReader reader)
        {
            value = reader.ReadBoolean();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(value);
        }

    } // class ReadyMessage

    public class CustomAddPlayerMessage : MessageBase
    {
        public string username;

        public CustomAddPlayerMessage()
        {
        }

        public CustomAddPlayerMessage(UserProfile user)
        {
            this.username = user.GetName();
        }

        public override void Deserialize(NetworkReader reader)
        {
            username = reader.ReadString();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(username);
        }
    
    } // class CustomAddPlayerMessage

    public class GameStateMessage : MessageBase
    {
        public DualNetworkManager.GameState newState;

        public GameStateMessage()
        {
        }

        public GameStateMessage(DualNetworkManager.GameState newState)
        {
            this.newState = newState;
        }

        public override void Deserialize(NetworkReader reader)
        {
            newState = (DualNetworkManager.GameState)reader.ReadInt32();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((int)newState);
        }
    }

    public class PrepareMessage : MessageWithExtra
    {
        public string map;

        public PrepareMessage()
        {
        }

        public PrepareMessage(string map, MessageBase extraMessage) : base(extraMessage)
        {
            this.map = map;
        }

        protected override void CustomSerialize(NetworkWriter writer)
        {
            writer.Write(map);
        }

        protected override void CustomDeserialize(NetworkReader reader)
        {
            map = reader.ReadString();
        }
    }

    public abstract class MessageWithExtra : MessageBase
    {
        NetworkReader reader;
        int msgSize;
        byte[] msgData;

        public MessageWithExtra()
        {
        }

        public MessageWithExtra(MessageBase extraMessage)
        {
            if(extraMessage != null)
            {
                NetworkWriter w = new NetworkWriter();
                extraMessage.Serialize(w);

                msgData = w.ToArray();
                msgSize = w.Position;
            }
        }

        public NetworkReader ExtraReader()
        {
            return reader;
        }

        public TMsg ReadExtraMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(reader);
            return msg;
        }

        public override void Deserialize(NetworkReader reader)
        {
            CustomDeserialize(reader);

            msgData = reader.ReadBytesAndSize();
            if(msgData == null)
            {
                msgSize = 0;
            }
            else
            {
                msgSize = msgData.Length;
            }

            reader = new NetworkReader(msgData);
        }

        public override void Serialize(NetworkWriter writer)
        {
            CustomSerialize(writer);

            writer.WriteBytesAndSize(msgData, msgSize);
        }

        protected abstract void CustomSerialize(NetworkWriter writer);
        protected abstract void CustomDeserialize(NetworkReader reader);

    } // class MessageWithExtra

    public class StatusMessage : MessageBase
    {
        public string map;
        public DualNetworkManager.GameState gameState;

        NetworkReader reader;
        int msgSize;
        byte[] msgData;


        public StatusMessage()
        {
        }

        public StatusMessage(string map, DualNetworkManager.GameState gameState, MessageBase extraMessage)
        {
            this.map = map;
            this.gameState = gameState;

            if (extraMessage != null)
            {
                NetworkWriter w = new NetworkWriter();
                extraMessage.Serialize(w);

                msgData = w.ToArray();
                msgSize = w.Position;
            }
        }
        
        public NetworkReader ExtraReader()
        {
            return reader;
        }

        public TMsg ReadExtraMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(reader);
            return msg;
        }

        public override void Deserialize(NetworkReader reader)
        {
            map = reader.ReadString();
            gameState = (DualNetworkManager.GameState)reader.ReadInt32();

            msgData = reader.ReadBytesAndSize();
            if(msgData == null)
            {
                msgSize = 0;
            }
            else
            {
                msgSize = msgData.Length;
            }

            reader = new NetworkReader(msgData);
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(map);
            writer.Write((int)gameState);

            writer.WriteBytesAndSize(msgData, msgSize);
        }

        public override string ToString()
        {
            return System.String.Format("[map: {0}, state: {1}]", map, gameState.ToString());
        }
    } // class StatusMessage

} // namespace Julo.Network