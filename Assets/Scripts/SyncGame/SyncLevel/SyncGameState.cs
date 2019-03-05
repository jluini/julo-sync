using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace SyncGame
{
    
    public class SyncGameState : MessageBase
    {
        public List<UnitState> unitsState;

        public SyncGameState()
        {
        }

        public SyncGameState(List<Unit> units)
        {
            unitsState = new List<UnitState>();

            foreach(Unit t in units)
            {
                var td = t.GetState();
                unitsState.Add(td);
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(unitsState.Count);

            foreach(UnitState unit in unitsState)
            {
                unit.Serialize(writer);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            int count = reader.ReadInt32();
            unitsState = new List<UnitState>();

            for(int i = 0; i < count; i++)
            {
                var newUnit = new UnitState();
                newUnit.Deserialize(reader);
                unitsState.Add(newUnit);
            }
        }
        
        public override string ToString()
        {
            var ret = System.String.Format("{0} units:\n", unitsState.Count);

            foreach(var unit in unitsState)
            {
                ret += System.String.Format("\n{0}\t{1}", unit.role, unit.index);
            }
            ret += "\n";

            return ret;
        }

    } // class GameState

} // namespace SyncGame
