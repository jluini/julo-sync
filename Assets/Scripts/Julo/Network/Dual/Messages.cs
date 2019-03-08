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
        public const short RemovePlayer = MsgTypeBase + 6;

        // Messaging
        public const short GameServerToClient = MsgTypeBase + 7;
        public const short GameClientToServer = MsgTypeBase + 8;

        public const short Highest = GameClientToServer;
    }
    
    public class ListOfMessages : MessageBase
    {
        public List<MessageBase> data;
        public int count;

        public NetworkReader dataReader;

        public ListOfMessages()
        {
            this.data = new List<MessageBase>();
        }

        public ListOfMessages(List<MessageBase> data)
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

        public void Add(MessageBase message)
        {
            data.Add(message);
        }

    } // class ListOfMessages
    
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
    
    public class DualPlayerSnapshot : MessageBase
    {
        public int connectionId;
        public short controllerId;

        public DualPlayerSnapshot()
        {
        }

        public DualPlayerSnapshot(int connectionId, short controllerId)
        {
            this.connectionId = connectionId;
            this.controllerId = controllerId;
        }

        public DualPlayerSnapshot(IPlayer player)
        {
            if(player == null)
            {
                this.connectionId = -1;
                this.controllerId = -1;
            }
            else
            {
                this.connectionId = player.ConnectionId();
                this.controllerId = player.ControllerId();
            }
        }
        
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(connectionId);
            writer.Write(controllerId);
        }

        public override void Deserialize(NetworkReader reader)
        {
            connectionId = reader.ReadInt32();
            controllerId = reader.ReadInt16();
        }

    } // class DualPlayerSnapshot
    
} // namespace Julo.Network
