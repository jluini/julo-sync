using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace Turtle
{
    
    public class GameState : MessageBase
    {
        public List<TurtleState> units;

        public GameState()
        {
        }

        public GameState(List<Turtle> turtles)
        {
            units = new List<TurtleState>();

            foreach(Turtle t in turtles)
            {
                var td = t.GetState();
                units.Add(td);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(units.Count);

            foreach(TurtleState unit in units)
            {
                unit.Serialize(writer);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            units = new List<TurtleState>();

            for(int i = 0; i < count; i++)
            {
                var newUnit = new TurtleState();
                newUnit.Deserialize(reader);
                units.Add(newUnit);
            }
        }

        public void ApplyTo(Dictionary<uint, Turtle> turtlesByNetId)
        {
            foreach(TurtleState d in units)
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

            foreach(var unit in units)
            {
                uint netId = unit.netId;
                ret += System.String.Format("\n{0}\t{1}\t{2}", netId, unit.role, unit.index);
            }
            ret += "\n";

            return ret;
        }

    } // class GameState

} // namespace Turtle
