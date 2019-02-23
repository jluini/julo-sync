using System.Collections;
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
        public static TurtleServer instance = null;

        public Turtle onlineTurtlePrefab;
        public Turtle offlineTurtlePrefab;

        List<Turtle>[] turtlesPerRole;
        Dictionary<uint, Turtle> turtlesByNetId = null;

        int expectedNumberOfTurtles = -1;

        protected override void OnStartServer()
        {
            base.OnStartServer();

            instance = this;
            Log.Debug("%%% TurtleServer::OnStartServer({0})", numRoles);

            turtlesPerRole = new List<Turtle>[numRoles];
            for(int r = 0; r < numRoles; r++)
            {
                turtlesPerRole[r] = new List<Turtle>();
            }
            
            if(mode == Mode.OnlineMode)
            {
                turtlesByNetId = new Dictionary<uint, Turtle>();
            }
        }

        protected override void SpawnInitialUnits()
        {
            if(mode == Mode.OfflineMode)
            {
                throw new System.NotImplementedException();
            }

            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();

            //Log.Debug("{0} spawn points found", spawnPoints.Length);

            //CheckSpawnPoints(spawnPoints);
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
            sortedSpawnPoints.Sort((x, y) => x.role  - y.role );
            foreach(var sp in sortedSpawnPoints)
            {
                Log.Debug("{0}, {1}", sp.role, sp.index);
            }

            foreach(var sp in sortedSpawnPoints)
            {
                if(sp.role > numRoles)
                {
                    Log.Warn("Spawn point role too high ({0} > {1})", sp.role, numRoles);
                    continue;
                }

                Turtle newTurtle;
                newTurtle = Object.Instantiate(onlineTurtlePrefab) as Turtle;

                newTurtle.transform.position = sp.transform.position;
                newTurtle.transform.rotation = sp.transform.rotation;

                newTurtle.role = sp.role;
                newTurtle.index = sp.index;

                GetTurtlesForRole(sp.role).Add(newTurtle); ;
            }

            // TODO do checks

            var initialTurtles = GetAllTurtles();
            expectedNumberOfTurtles = initialTurtles.Count;

            if(mode == Mode.OnlineMode)
            {
                foreach(Turtle t in initialTurtles)
                {
                    NetworkServer.Spawn(t.gameObject);
                }
            }
        }

        List<Turtle> GetTurtlesForRole(int role)
        {
            // TODO checks!
            return turtlesPerRole[role - 1];
        }
        public void RegisterInServer(Turtle t)
        {
            if(mode == Mode.OfflineMode)
            {
                return;
            }
            var ni = t.GetComponent<NetworkIdentity>();
            uint netId = ni == null ? 10000 : ni.netId.Value;

            if(netId > 0)
            {
                if(turtlesByNetId == null)
                {
                    Log.Error("Dict not initialized");
                    return;
                }
                else if(turtlesByNetId.ContainsKey(netId))
                {
                    Log.Error("Turtle {0} already registered", netId);
                    return;
                }
                turtlesByNetId[netId] = t;
            }

            if(turtlesByNetId.Count == expectedNumberOfTurtles)
            {
                InitialUnitsWereSpawned();
            }
        }

        protected override bool RoleIsAlive(int numRole)
        {
            return GetTurtlesForRole(numRole).FindAll(t => !t.dead).Count > 0;
        }
        
        // TODO this is duplicated in TurtleClient
        public override MessageBase GetStateMessage()
        {
            return new GameState(GetAllTurtles());
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
        
        void CheckSpawnPoints(SpawnPoint[] spawnPoints)
        {
            // TODO
        }

        public override void OnMessage(WrappedMessage message, int from)
        {
            short msgType = message.messageType;

            if(msgType == Julo.TurnBased.MsgType.GameState)
            {
                // TODO could be more direct?

                var msg = message.ReadExtraMessage<GameState>();

                SendToAllBut(from, Julo.TurnBased.MsgType.GameState, msg);
            }
            else
            {
                base.OnMessage(message, from);
            }
        }

    } // class TurtleServer

} // namespace Turtle

