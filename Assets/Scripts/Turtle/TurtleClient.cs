using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking; // TODO depends on network?

using Julo.Logging;
using Julo.Network;
using Julo.TurnBased;

namespace Turtle
{
    public class TurtleClient : TurnBasedClient
    {
        public Turtle onlineTurtlePrefab;

        public static TurtleClient instance = null;
        public static float torque = 5f;

        List<Turtle>[] turtlesPerRole;
        Dictionary<uint, Turtle> turtlesByNetId;
        /*
        Dictionary<uint, TurtleData> dataByNetId;
        */
        //bool thisClientIsPlaying = false;
        //TBPlayer targetPlayer = null;
        Turtle targetTurtle = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType tt = TurnType.None;
        bool turnEnded = true;

        public override void OnStartClient()
        {
            Log.Debug("$$$$ TurtleClient::OnStartClient");
            instance = this;
            turtlesPerRole = new List<Turtle>[numRoles];
            turtlesByNetId = new Dictionary<uint, Turtle>();
            /*

            if(mode == Mode.OnlineMode)
            {
                var initialStateMessage = extraMessage.ReadMessage<TurtleStateMessage>();
                
                Debug.Log("$$$$ INIT");
                Debug.Log(initialStateMessage);
                turtlesByNetId = new Dictionary<uint, Turtle>();
            }
            */
        }

        /*
        GameObject SpawnTurtleInClient(Vector3 position, NetworkHash128 assetId)
        {
            Log.Debug("%%% SPAWN %%% ({0})");

            var ret = Instantiate(onlineTurtlePrefab) as Turtle;

            uint netId = ret.GetComponent<NetworkIdentity>().netId.Value;

            Log.Debug("%%% netId={0}", netId);

            // TODO probably netId it's not set :(

            // TODO register turtle by net id....

            if(!dataByNetId.ContainsKey(netId))
            {
                Log.Warn("Not in initial state");
            }
            else
            {
                TurtleData data = dataByNetId[netId];

                ret.role = data.role;
                ret.index = data.index;

                ret.transform.position = data.position;
                ret.transform.rotation = data.rotation;

                dataByNetId.Remove(netId);

                if(dataByNetId.Count == 0)
                {
                    Log.Debug("All are spawed!!!");

                    DualNetworkManager.instance.SpawnOkCommand();

                }
            }

            turtlesByNetId[netId] = ret;

            return ret.gameObject;
        }

        void UnSpawnTurtleInClient(GameObject spawned)
        {
            Log.Debug("%%% UNSPAWN %%%");
            Destroy(spawned);
        }
        */
        Turtle GetNextTurtle(List<Turtle> turtles)
        {
            // TODO criteria to pick turtle
            if(turtles.Count > 0)
            {
                return turtles[0];
            }
            Log.Error("No turtles");
            return null;
        }
        
        public void RegisterTurtle(Turtle turtle)
        {
            Log.Debug("Registering turtle {0}/{1}", turtle.role, turtle.index);
            
            // TODO do this in offline mode?
            int role = turtle.role;

            if(role < 1 || role > numRoles)
            {
                Log.Warn("Invalid client turtle role: {0}", role);
                return;
            }

            if(turtlesPerRole[role - 1] == null)
            {
                turtlesPerRole[role - 1] = new List<Turtle>();
            }

            turtlesPerRole[role - 1].Add(turtle);

            if(mode == Mode.OnlineMode)
            {
                uint netId = turtle.GetComponent<NetworkIdentity>().netId.Value;
                if(turtlesByNetId.ContainsKey(netId))
                {
                    Log.Error("Turtle with netId {0} already registered!", netId);
                    return;
                }
                //Debug.Log("$$$$ POPULATE");
                turtlesByNetId.Add(netId, turtle);
            }
        }
        // TODO what if it's offline?
        public override void StartGame(NetworkReader messageReader)
        {
        }

        public override void LateJoinGame(NetworkReader messageReader)
        {
            /*
            if(messageReader == null)
            {
                Log.Debug("Me acoplo sin data");
            }
            else
            {
                var stateMessage = messageReader.ReadMessage<TurtleStateMessage>();
                Log.Debug("Me acoplo y lo sé todo: {0} tortugas", stateMessage.data.Count);
                stateMessage.ApplyTo(turtlesByNetId);
            }
            */
        }

        protected override void OnStartTurn(TBPlayer player)
        {
            if(tt != TurnType.None)
            {
                Log.Warn("Unexpected TurnType {0} (A)", tt);
            }

            int role = player.GetRole();

            var turtles = turtlesPerRole[role - 1];

            if(turtles == null || turtles.Count == 0)
            {
                Log.Warn("No turtles with role {0}", role);
                tt = TurnType.Wait;
                turnEnded = false;
                StartCoroutine(EndTurnDelayed());
                return;
            }

            tt = TurnType.Keyboard;

            Log.Debug("It's my turn here, I have {0} turtles", turtles.Count);

            targetTurtle = GetNextTurtle(turtles);

            targetTurtle.SetPlaying(true);
        }

        protected override bool TurnIsOn()
        {
            if(tt == TurnType.Wait)
            {
                if(turnEnded)
                {
                    tt = TurnType.None;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if(tt != TurnType.Keyboard)
            {
                Log.Warn("Unexpected TurnType {0} (A)", tt);
                return false;
            }

            float hor = Input.GetAxis("Horizontal");
            float fire3 = Input.GetAxis("Fire3");

            if(hor > 0)
            {
                targetTurtle.AddTorque(-torque);
            }
            else if(hor < 0)
            {
                targetTurtle.AddTorque(+torque);
            }

            if(fire3 > 0)
            {
                targetTurtle.SetPlaying(false);
                tt = TurnType.None;
                return false;
            }

            return true;
        }

        IEnumerator EndTurnDelayed()
        {
            yield return new WaitForSecondsRealtime(2f);
            turnEnded = true;
        }

        public override MessageBase GetStateMessage()
        {
            return new TurtleStateMessage(GetAllTurtles());
        }
        // TODO getting state is duplicated between TurtleClient and TurtleServer
        public List<Turtle> GetAllTurtles()
        {
            List<Turtle> ret = new List<Turtle>();

            for(int i = 1; i <= numRoles; i++)
            {
                foreach(Turtle t in turtlesPerRole[i])
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

    } // class TurtleClient

} // namespace Turtle

