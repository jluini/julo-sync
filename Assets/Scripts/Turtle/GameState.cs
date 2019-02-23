using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace Turtle
{
    
    public class GameState : MessageBase
    {
        public Dictionary<uint, TurtleState> units;

        public GameState()
        {
        }

        public GameState(List<Turtle> turtles)
        {
            units = new Dictionary<uint, TurtleState>();
            foreach(Turtle t in turtles)
            {
                var td = t.GetState();
                units.Add(td.netId, td);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(units.Count);

            foreach(TurtleState unit in units.Values)
            {
                unit.Serialize(writer);

                /*
                writer.Write(unit.netId);
                writer.Write(unit.role);
                writer.Write(unit.index);
                writer.Write(unit.position);
                writer.Write(unit.rotation);
                writer.Write(unit.velocity);
                writer.Write(unit.angularVelocity);
                */
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            units = new Dictionary<uint, TurtleState>();

            for(int i = 0; i < count; i++)
            {
                var newUnit = new TurtleState();
                newUnit.Deserialize(reader);
                /*
                var newUnit = new TurtleState(
                    reader.ReadUInt32(),          // netId
                    reader.ReadInt32(),           // role
                    reader.ReadInt32(),           // index
                    reader.ReadVector2(),         // position
                    reader.ReadQuaternion(),      // rotation
                    reader.ReadVector2(),         // velocity
                    reader.ReadSingle()           // angularVelocity
                );
                */
                units.Add(newUnit.netId, newUnit);
            }
        }

        public void ApplyTo(Dictionary<uint, Turtle> turtlesByNetId)
        {
            foreach(TurtleState d in units.Values)
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
            var ret = System.String.Format("{0} turtles:\n", units.Count);

            foreach(uint netId in units.Keys)
            {
                var d = units[netId];
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

    } // class GameState

} // namespace Turtle
