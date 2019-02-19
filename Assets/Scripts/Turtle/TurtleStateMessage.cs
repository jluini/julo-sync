using System.Collections.Generic;

using UnityEngine.Networking;

namespace Turtle
{
    
    public class TurtleStateMessage : MessageBase
    {
        public List<TurtleData> data;

        public TurtleStateMessage()
        {
        }
        public TurtleStateMessage(List<Turtle> turtles)
        {
            data = new List<TurtleData>();
            foreach(Turtle t in turtles)
            {
                data.Add(new TurtleData(t));
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(data.Count);

            foreach(TurtleData datum in data)
            {
                writer.Write(datum.netId);
                writer.Write(datum.role);
                writer.Write(datum.index);
                writer.Write(datum.position);
                writer.Write(datum.rotation);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            data = new List<TurtleData>();

            for(int i = 0; i < count; i++)
            {
                var newDatum = new TurtleData();
                newDatum.netId = reader.ReadUInt32();
                newDatum.role = reader.ReadInt32();
                newDatum.index = reader.ReadInt32();
                newDatum.position = reader.ReadVector3();
                newDatum.rotation = reader.ReadQuaternion();
                data.Add(newDatum);
            }
        }

        } // class TurtleStateMessage

} // namespace Turtle
