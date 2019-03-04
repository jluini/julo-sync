using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace TurtleGame
{
    
    public class TurtleGameState : MessageBase
    {
        public List<TurtleState> units;

        public TurtleGameState()
        {
        }

        public TurtleGameState(List<Turtle> turtles)
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
        
        public override string ToString()
        {
            var ret = System.String.Format("{0} turtles:\n", units.Count);

            foreach(var unit in units)
            {
                ret += System.String.Format("\n{0}\t{1}", unit.role, unit.index);
            }
            ret += "\n";

            return ret;
        }

    } // class GameState

} // namespace Turtle
