using System.Collections.Generic;

using UnityEngine.Networking;

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

        // Sent from server to clients to inform initial game state
        public const short InitialStatus = MsgTypeBase + 2;

        // STARTING GAME

        // Sent from server to clients
        //public const short GameWillStart = MsgTypeBase + 3;

        // Sent from server to clients to deliver initial game state
        //public const short StartGame = MsgTypeBase + 4;

        // Sent from clients to server after Prepare
        //public const short ReadyToStart = MsgTypeBase + 5;

        public const short GameServerToClient = MsgTypeBase + 6;
        public const short GameClientToServer = MsgTypeBase + 7;

        public const short Highest = GameClientToServer;
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
    /*
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
    */
    /*
    public class StartGameMessage : MessageBase
    {
        public string scene;
        public List<MessageBase> initialData;

        public NetworkReader initialDataReader;

        public StartGameMessage()
        {
        }

        public StartGameMessage(string scene, List<MessageBase> initialData)
        {
            this.scene = scene;
            this.initialData = initialData;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(scene);

            writer.Write(initialData.Count);

            foreach(var m in initialData)
            {
                writer.Write(m);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            scene = reader.ReadString();

            int count = reader.ReadInt32();

            this.initialDataReader = reader;
        }

        public TMsg ReadInitialMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(initialDataReader);
            return msg;
        }

    } // class StartGameMessage
    */
    public class StartRemoteClientMessage : MessageBase
    {
        public bool accepted; // TODO can assume it is true?
        //public string sceneName; // TOTO necessary?
        public List<MessageBase> data;
        public int count;

        public NetworkReader dataReader;

        public StartRemoteClientMessage()
        {
        }

        public StartRemoteClientMessage(bool accepted, /*string sceneName, */List<MessageBase> data)
        {
            this.accepted = accepted;
            // this.sceneName = sceneName;
            this.data = data;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(accepted);
            //writer.Write(sceneName);
            writer.Write(data.Count);

            foreach(var m in data)
            {
                writer.Write(m);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            accepted = reader.ReadBoolean();
            //sceneName = reader.ReadString();
            count = reader.ReadInt32();
            dataReader = reader;
        }

        public TMsg ReadInitialMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(dataReader);
            return msg;
        }

    } // class StartRemoteClientMessage
    /*
    public class StatusMessage : MessageBase
    {
        public bool accepted;
        public string scene;
        public DualNetworkManager.GameState gameState;

        NetworkReader extraReader;
        int msgSize;
        byte[] msgData;


        public StatusMessage()
        {
        }

        public StatusMessage(bool accepted, string scene, DualNetworkManager.GameState gameState, MessageBase extraMessage)
        {
            this.accepted = accepted;
            this.scene = scene;
            this.gameState = gameState;

            if (extraMessage != null)
            {
                NetworkWriter w = new NetworkWriter();
                extraMessage.Serialize(w);

                msgData = w.ToArray();
                msgSize = w.Position;

                extraReader = new NetworkReader(msgData);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(accepted);
            writer.Write(scene);
            writer.Write((int)gameState);

            writer.WriteBytesAndSize(msgData, msgSize);
        }

        public override void Deserialize(NetworkReader reader)
        {
            accepted = reader.ReadBoolean();
            scene = reader.ReadString();
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

            extraReader = new NetworkReader(msgData);
        }
        
        public NetworkReader ExtraReader()
        {
            return extraReader;
        }

        public TMsg ReadExtraMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(extraReader);
            return msg;
        }

        public override string ToString()
        {
            return System.String.Format("[scene: {0}, state: {1}]", scene, gameState.ToString());
        }
    } // class StatusMessage
    */
    public class WrappedMessage : MessageBase
    {
        public short messageType;

        NetworkReader extraReader;
        int msgSize;
        byte[] msgData;


        public WrappedMessage()
        {
        }

        public WrappedMessage(short messageType, MessageBase extraMessage)
        {
            this.messageType = messageType;

            if(extraMessage != null)
            {
                NetworkWriter w = new NetworkWriter();
                extraMessage.Serialize(w);

                msgData = w.ToArray();
                msgSize = w.Position;

                extraReader = new NetworkReader(msgData);
            }
        }

        public NetworkReader ExtraReader()
        {
            return extraReader;
        }

        public TMsg ReadExtraMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(extraReader);
            return msg;
        }

        public override void Deserialize(NetworkReader reader)
        {
            messageType = reader.ReadInt16();

            msgData = reader.ReadBytesAndSize();
            if(msgData == null)
            {
                msgSize = 0;
            }
            else
            {
                msgSize = msgData.Length;
            }

            extraReader = new NetworkReader(msgData);
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(messageType);

            writer.WriteBytesAndSize(msgData, msgSize);
        }

    } // class WrappedMessage


} // namespace Julo.Network
