using System.Collections.Generic;

using UnityEngine.Networking;

namespace SyncGame
{

    public class MsgType
    {
        const short MsgTypeBase = Julo.TurnBased.MsgType.Highest;

        public const short ServerUpdate = MsgTypeBase + 1;
        public const short ClientUpdate = MsgTypeBase + 2;

        public const short Highest = ClientUpdate;
    }

    public class SyncMatchSnapshot : MessageBase
    {
        public List<UnitState> unitsState;

        public SyncMatchSnapshot()
        {
        }

        public SyncMatchSnapshot(List<Unit> units)
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

    } // class SyncMatchSnapshot

} // namespace SyncGame
