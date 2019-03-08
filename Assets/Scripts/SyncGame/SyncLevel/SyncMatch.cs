using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;

namespace SyncGame
{

    public class SyncMatch
    {
        int numRoles;
        List<Unit>[] unitsPerRole;

        public void CreateFromSpawnPoints(int numRoles, Unit unitModel, SpawnPoint[] spawnPoints)
        {
            Init(numRoles);

            var sortedSpawnPoints = new List<SpawnPoint>(spawnPoints);

            sortedSpawnPoints.Sort((x, y) =>
            {
                var ret = x.role - y.role;
                if(ret == 0)
                {
                    ret = x.index - y.index;
                }
                return ret;
            });

            foreach(var sp in sortedSpawnPoints)
            {
                if(sp.role > numRoles)
                {
                    Log.Warn("Spawn point role too high ({0} > {1})", sp.role, numRoles);
                    continue;
                }

                CreateUnit(unitModel, sp.role, sp.index, sp.transform.position, sp.transform.rotation);
            }
        }

        public void CreateFromInitialState(int numRoles, Unit unitModel, SyncMatchSnapshot snapshot)
        {
            Init(numRoles);

            foreach(var ts in snapshot.unitsState)
            {
                CreateUnit(unitModel, ts.role, ts.index, ts.position, ts.rotation);
            }
        }
        
        public SyncMatchSnapshot GetSnapshot()
        {
            return new SyncMatchSnapshot(GetAllUnits());
        }
        
        public void UpdateState(SyncMatchSnapshot snapshot)
        {
            if(unitsPerRole == null)
            {
                Log.Warn("Not initialized yet");
                return;
            }

            foreach(var unitState in snapshot.unitsState)
            {
                var unit = GetUnitFor(unitState);
                unit.SetState(unitState);
            }
        }

        Unit GetUnitFor(UnitState state)
        {
            int role = state.role;
            int index = state.index;
            List<Unit> units = GetUnitsForRole(role);

            for(int i = 0; i < units.Count; i++)
            {
                var unit = units[i];

                if(unit.index == index)
                {
                    if(i != index - 1)
                    {
                        Log.Warn("Unit with index {0} ({1}) is in position {2}", index, index - 1, i);
                    }
                    return unit;
                }
            }

            Log.Error("Unit {0}:{1} not found!! ({2} in role)", role, index, units.Count);
            return null;
        }


        public List<Unit> GetAllUnits()
        {
            List<Unit> ret = new List<Unit>();

            for(int i = 1; i <= numRoles; i++)
            {
                foreach(Unit t in GetUnitsForRole(i))
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

        public List<Unit> GetUnitsForRole(int role)
        {
            if(role < 1 || role > unitsPerRole.Length)
            {
                Log.Error("Role {0} not found", role);
                return new List<Unit>();
            }
            // TODO check
            return unitsPerRole[role - 1];
        }

        ///////////// Utils

        void Init(int numRoles)
        {
            this.numRoles = numRoles;
            unitsPerRole = new List<Unit>[numRoles];

            for(int r = 1; r <= numRoles; r++)
            {
                unitsPerRole[r - 1] = new List<Unit>();
            }
        }

        void CreateUnit(Unit unitModel, int role, int index, Vector2 position, Quaternion rotation)
        {
            Unit newUnit;
            newUnit = Object.Instantiate(unitModel) as Unit;

            newUnit.role = role;
            newUnit.index = index;

            newUnit.transform.position = position;
            newUnit.transform.rotation = rotation;

            GetUnitsForRole(role).Add(newUnit);
        }

    } // class SyncMatch

} // namespace SyncGame
