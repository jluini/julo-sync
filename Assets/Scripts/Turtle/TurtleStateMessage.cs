using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Turtle
{
    
    public class TurtleStateMessage : MessageBase
    {
        //public List<TurtleData> data;
        public Dictionary<uint, TurtleData> data;

        public TurtleStateMessage()
        {
        }
        public TurtleStateMessage(List<Turtle> turtles)
        {
            data = new Dictionary<uint, TurtleData>();
            foreach(Turtle t in turtles)
            {
                var td = t.GetState();
                data.Add(td.netId, td);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(data.Count);

            foreach(TurtleData datum in data.Values)
            {
                writer.Write(datum.netId);
                writer.Write(datum.role);
                writer.Write(datum.index);
                writer.Write(datum.position);
                writer.Write(datum.rotation);
                writer.Write(datum.velocity);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            data = new Dictionary<uint, TurtleData>();

            for(int i = 0; i < count; i++)
            {
                var newDatum = new TurtleData(
                    reader.ReadUInt32(),          // netId
                    reader.ReadInt32(),           // role
                    reader.ReadInt32(),           // index
                    reader.ReadVector3(),         // position
                    reader.ReadQuaternion(),      // rotation
                    reader.ReadVector2()          // velocity
                );

                data.Add(newDatum.netId, newDatum);
            }
        }

        public void ApplyTo(Dictionary<uint, Turtle> turtlesByNetId)
        {
            foreach(TurtleData d in data.Values)
            {
                if(!turtlesByNetId.ContainsKey(d.netId))
                {
                    Log.Error("###### netId {0} not found in dict with {1}", d.netId, turtlesByNetId.Count);
                    //Log.Debug("{0}", turtlesByNetId);
                    continue;
                }

                Turtle t = turtlesByNetId[d.netId];
                t.SetState(d);
            }
        }

        public override string ToString()
        {
            var ret = System.String.Format("{0} turtles:\n", data.Count);

            foreach(uint netId in data.Keys)
            {
                var d = data[netId];
                ret += System.String.Format("\n{0}\t{1}\t{2}", netId, d.role, d.index);
                /*
                ret += "\n";
                ret += netId.ToString();
                ret += "\t";
                ret += role.ToString();
                ret += "\t";
                ret += index.ToString();
                */
            }
            ret += "\n";

            return ret;
        }

    } // class TurtleStateMessage

} // namespace Turtle
