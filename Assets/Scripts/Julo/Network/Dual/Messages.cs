using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Users;
using Julo.Logging;

namespace Julo.Network
{

    public class MsgType
    {
        const short MsgTypeBase = UnityEngine.Networking.MsgType.Highest;


        // Connecting
        public const short ConnectionAccepted = MsgTypeBase + 1;
        // public const short ConnectionRejected = MsgTypeBase + 2;
        public const short InitialStateRequest = MsgTypeBase + 3;
        public const short InitialState = MsgTypeBase + 4;

        // Players
        public const short NewPlayer = MsgTypeBase + 5;

        // Messaging
        public const short GameServerToClient = MsgTypeBase + 6;
        public const short GameClientToServer = MsgTypeBase + 7;

        public const short Highest = GameClientToServer;
    }
    
    public class MessageStackMessage : MessageBase
    {
        public List<MessageBase> data;
        public int count;

        public NetworkReader dataReader;

        public MessageStackMessage()
        {
        }

        public MessageStackMessage(List<MessageBase> data)
        {
            this.data = data;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(data.Count);

            foreach(var m in data)
            {
                writer.Write(m);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            count = reader.ReadInt32();
            dataReader = reader;
        }

        public TMsg ReadMessage<TMsg>() where TMsg : MessageBase, new()
        {
            var msg = new TMsg();
            msg.Deserialize(dataReader);
            return msg;
        }

    } // class MessageStackMessage

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

        public TMsg ReadInternalMessage<TMsg>() where TMsg : MessageBase, new()
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

    ////////////

    // TODO rename to DualPlayerScreenshot
    // implement IDualPlayer?
    public class DualPlayerMessage : MessageBase, IDualPlayer
    {
        public uint playerId;
        public int connectionId;
        public short controllerId;

        public DualPlayerMessage()
        {
        }

        public DualPlayerMessage(IDualPlayer player)
        {
            this.playerId = player.PlayerId();
            this.connectionId = player.ConnectionId();
            this.controllerId = player.ControllerId();
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(playerId);
            writer.Write(connectionId);
            writer.Write(controllerId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            playerId = reader.ReadUInt32();
            connectionId = reader.ReadInt32();
            controllerId = reader.ReadInt16();
        }

        public uint PlayerId()
        {
            return playerId;
        }

        public int ConnectionId()
        {
            return controllerId;
        }

        public short ControllerId()
        {
            return controllerId;
        }

        public bool IsLocal()
        {
            throw new System.Exception();
        }

        public void AddListener(IDualPlayerListener listener)
        {
            throw new System.Exception();
        }

    } // class DualPlayerMessage

} // namespace Julo.Network
