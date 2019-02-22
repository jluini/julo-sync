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
                var td = new TurtleData(t);
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
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            data = new Dictionary<uint, TurtleData>();

            for(int i = 0; i < count; i++)
            {
                var newDatum = new TurtleData();
                newDatum.netId = reader.ReadUInt32();
                newDatum.role = reader.ReadInt32();
                newDatum.index = reader.ReadInt32();
                newDatum.position = reader.ReadVector3();
                newDatum.rotation = reader.ReadQuaternion();
                data.Add(newDatum.netId, newDatum);
            }
        }

        public void ApplyTo(Dictionary<uint, Turtle> turtlesByNetId)
        {
            Debug.Log("Applying state");

            foreach(TurtleData d in data.Values)
            {
                Log.Debug("{0}: {1}/{2}", d.netId, d.role, d.index);

                //var go = ClientScene.FindLocalObject(new NetworkInstanceId(d.netId));
                //Turtle t = go.GetComponent<Turtle>();

                if(!turtlesByNetId.ContainsKey(d.netId))
                {
                    Log.Error("###### netId {0} not found in dict with {1}", d.netId, turtlesByNetId.Count);
                    Log.Debug("{0}", turtlesByNetId);
                    continue;
                }
                Turtle t = turtlesByNetId[d.netId];

                t.role = d.role;
                t.index = d.index;

                t.gameObject.transform.position = d.position;
                t.gameObject.transform.rotation = d.rotation;
            }
        }

        public override string ToString()
        {
            var ret = "";

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
