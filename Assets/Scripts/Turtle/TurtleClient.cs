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
        public static TurtleClient instance = null;
        public static float torque = 5f;

        Dictionary<int, List<Turtle>> turtlesPerRole;

        //bool thisClientIsPlaying = false;
        //TBPlayer targetPlayer = null;
        Turtle targetTurtle = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType tt = TurnType.None;
        bool turnEnded = true;

        public override void OnStartClient()
        {
            instance = this;

            turtlesPerRole = new Dictionary<int, List<Turtle>>();
        }
        
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
            //Log.Debug("Registering turtle {0}/{1}", turtle.role, turtle.index);

            int role = turtle.role;

            if(role < 1 || role > numRoles)
            {
                Log.Warn("Invalid client turtle role: {0}", role);
            }

            if(!turtlesPerRole.ContainsKey(role))
            {
                turtlesPerRole.Add(role, new List<Turtle>());
            }

            turtlesPerRole[role].Add(turtle);
        }
        
        // TODO what if it's offline?
        public override void StartGame(NetworkReader messageReader)
        {
            var msg = messageReader.ReadMessage<TurtleStateMessage>();
            Log.Debug("Empieza y lo sé todo: {0} tortugas", msg.data.Count);

            if (isHosted)
            {
                Log.Debug("No actualizo nada pues soy hosted ;)");
            }
            else
            {
                foreach (TurtleData d in msg.data)
                {
                    Log.Debug("{0}: {1}  &&&  {2}", d.netId, d.position, d.rotation);

                    var go = ClientScene.FindLocalObject(new NetworkInstanceId(d.netId));
                    Turtle t = go.GetComponent<Turtle>();

                    t.role = d.role;
                    t.index = d.index;

                    Vector3 newPos = d.position;
                    newPos.y += 10f;
                    go.transform.position = newPos;
                    go.transform.rotation = d.rotation;
                }
            }
        }

        public override void LateJoinGame(NetworkReader messageReader)
        {
            if(messageReader == null)
            {
                Log.Debug("Me acoplo sin data");
            }
            else
            {
                var msg = messageReader.ReadMessage<TurtleStateMessage>();
                Log.Debug("Me acoplo y lo sé todo: {0} tortugas", msg.data.Count);
            }
        }

        protected override void OnStartTurn(TBPlayer player)
        {
            if(tt != TurnType.None)
            {
                Log.Warn("Unexpected TurnType {0} (A)", tt);
            }

            int role = player.GetRole();

            if(!turtlesPerRole.ContainsKey(role))
            {
                Log.Warn("No turtles with role {0}", role);
                tt = TurnType.Wait;
                turnEnded = false;
                StartCoroutine(EndTurnDelayed());
                return;
            }

            tt = TurnType.Keyboard;

            List<Turtle> turtles = turtlesPerRole[role];
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

    } // class TurtleClient

} // namespace Turtle

