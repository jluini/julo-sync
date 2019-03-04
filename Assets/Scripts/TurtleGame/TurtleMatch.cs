using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;

namespace TurtleGame
{

    public class TurtleMatch
    {
        int numRoles;
        List<Turtle>[] turtlesPerRole;

        public void CreateFromSpawnPoints(int numRoles, Turtle turtleModel, SpawnPoint[] spawnPoints)
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

                CreateTurtle(turtleModel, sp.role, sp.index, sp.transform.position, sp.transform.rotation);
            }
        }

        public void CreateFromInitialState(int numRoles, Turtle turtleModel, TurtleGameState initialState)
        {
            Init(numRoles);

            foreach(var ts in initialState.units)
            {
                CreateTurtle(turtleModel, ts.role, ts.index, ts.position, ts.rotation);
            }
        }

        public TurtleGameState GetState()
        {
            return new TurtleGameState(GetAllTurtles());
        }

        public void UpdateState(TurtleGameState newState)
        {
            if(turtlesPerRole == null)
            {
                Log.Warn("Not initialized yet");
                return;
            }

            foreach(var turtleState in newState.units)
            {
                var turtle = GetTurtleFor(turtleState);
                turtle.SetState(turtleState);
            }
        }

        Turtle GetTurtleFor(TurtleState state)
        {
            int role = state.role;
            int index = state.index;
            List<Turtle> turtles = GetTurtlesForRole(role);

            for(int i = 0; i < turtles.Count; i++)
            {
                var turtle = turtles[i];

                if(turtle.index == index)
                {
                    if(i != index - 1)
                    {
                        Log.Warn("Turtle with index {0} ({1}) is in position {2}", index, index - 1, i);
                    }
                    return turtle;
                }
            }

            Log.Error("Turtle {0}:{1} not found!! ({2} in role)", role, index, turtles.Count);
            return null;
        }


        public List<Turtle> GetAllTurtles()
        {
            List<Turtle> ret = new List<Turtle>();

            for(int i = 1; i <= numRoles; i++)
            {
                foreach(Turtle t in GetTurtlesForRole(i))
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

        public List<Turtle> GetTurtlesForRole(int role)
        {
            // TODO check
            return turtlesPerRole[role - 1];
        }

        ///////////// Utils

        void Init(int numRoles)
        {
            this.numRoles = numRoles;
            turtlesPerRole = new List<Turtle>[numRoles];

            for(int r = 1; r <= numRoles; r++)
            {
                turtlesPerRole[r - 1] = new List<Turtle>();
            }
        }

        void CreateTurtle(Turtle turtleModel, int role, int index, Vector2 position, Quaternion rotation)
        {
            Turtle newTurtle;
            newTurtle = Object.Instantiate(turtleModel) as Turtle;

            newTurtle.role = role;
            newTurtle.index = index;

            newTurtle.transform.position = position;
            newTurtle.transform.rotation = rotation;

            GetTurtlesForRole(role).Add(newTurtle);
        }

    } // class TurtleMatch

} // namespace TurtleGame
