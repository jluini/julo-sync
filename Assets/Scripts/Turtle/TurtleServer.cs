using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;
using Julo.TurnBased;

namespace Turtle
{
    public class TurtleServer : TurnBasedServer
    {

        public Turtle onlineTurtlePrefab;
        public Turtle offlineTurtlePrefab;

        List<Turtle>[] turtlesPerRole;

        public override void OnStartGame()
        {

            turtlesPerRole = new List<Turtle>[numRoles];

            for(int r = 0; r < numRoles; r++)
            {
                turtlesPerRole[r] = new List<Turtle>();
            }

            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();

            CheckSpawnPoints(spawnPoints);

            foreach(var sp in spawnPoints)
            {
                if(sp.role > numRoles)
                {
                    Log.Warn("Spawn point role too high ({0} > {1})", sp.role, numRoles);
                    continue;
                }

                Turtle newTurtle;
                //if(mode == Mode.OfflineMode)
                //{
                //    newTurtle = Object.Instantiate(offlineTurtlePrefab) as Turtle;
                //}
                //else
                //{
                    newTurtle = Object.Instantiate(onlineTurtlePrefab) as Turtle;
                //}

                newTurtle.transform.position = sp.transform.position;
                newTurtle.transform.rotation = sp.transform.rotation;

                newTurtle.role = sp.role;
                newTurtle.index = sp.index;

                turtlesPerRole[sp.role - 1].Add(newTurtle);

                if(mode == Mode.OnlineMode)
                {
                    NetworkServer.Spawn(newTurtle.gameObject);
                }
            }
        }

        public override bool RoleIsAlive(int numRole)
        {
            return true; // TODO
        }

        public List<Turtle> GetAllTurtles()
        {
            List<Turtle> ret = new List<Turtle>();

            for(int i = 1; i <= numRoles; i++)
            {
                foreach(Turtle t in turtlesPerRole[i - 1])
                {
                    ret.Add(t);
                }
            }

            return ret;
        }
        public override MessageBase GetStatusMessage()
        {
            return new TurtleStateMessage(GetAllTurtles());

            //return new UnityEngine.Networking.NetworkSystem.StringMessage("La vida loca");
        }



        void CheckSpawnPoints(SpawnPoint[] spawnPoints)
        {
            // TODO
        }

    } // class TurtleServer

} // namespace Turtle

