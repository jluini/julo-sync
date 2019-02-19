using UnityEngine.Networking;

using Julo.Logging;
using Julo.Users;

namespace Julo.Network
{

    public class MsgType
    {
        // Sent from clients to server to mark themselves as ready/not-ready
        public const short ClientSetReady = UnityEngine.Networking.MsgType.Highest + 1;

        // Sent from clients to server
        public const short StatusRequest = UnityEngine.Networking.MsgType.Highest + 2;

        // Sent from server to clients to inform initial game state
        public const short InitialStatus = UnityEngine.Networking.MsgType.Highest + 3;

        // Sent from server to clients
        public const short GameWillStart = UnityEngine.Networking.MsgType.Highest + 4;

        // Sent from server to clients
        public const short ReadyToStart = UnityEngine.Networking.MsgType.Highest + 5;

        // Sent from server to clients
        public const short GameStarted = UnityEngine.Networking.MsgType.Highest + 6;

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

    //public class MessageWithExtra : MessageBase

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

    /*
    public class MessageWithExtra : MessageBase
    {
        public MessageBase extraMessage;


    }
    */

} // namespace Julo.Network