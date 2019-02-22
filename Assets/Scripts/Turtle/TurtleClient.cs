﻿using System.Collections;
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

        public Turtle onlineTurtlePrefab;

        public static float torque = 5f;

        List<Turtle>[] turtlesPerRole;
        Dictionary<uint, Turtle> turtlesByNetId;
        Turtle targetTurtle = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType tt = TurnType.None;
        bool turnEnded = true;

        public override void OnStartClient()
        {
            instance = this;
            turtlesPerRole = new List<Turtle>[numRoles];
            turtlesByNetId = new Dictionary<uint, Turtle>();
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

            // Log.Debug("It's my turn here, I have {0} turtles", turtles.Count);

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
                foreach(Turtle t in turtlesPerRole[i - 1])
                {
                    ret.Add(t);
                }
            }

            return ret;
        }

        public override void OnMessage(WrappedMessage message)
        {
            short msgType = message.messageType;

            if(msgType == Julo.TurnBased.MsgType.InitialState)
            {
                var msg = message.ReadExtraMessage<TurtleStateMessage>();

                RegisterTurtles(msg.data);

                if(!isHosted)
                {
                    msg.ApplyTo(turtlesByNetId);
                }
            }
            else if(msgType == Julo.TurnBased.MsgType.GameState)
            {
                if(tt != TurnType.None)
                {
                    Log.Error("Recibí update pero estoy jugando yo!!");
                    return;
                }

                var msg = message.ReadExtraMessage<TurtleStateMessage>();

                msg.ApplyTo(turtlesByNetId);
            }
            else
            {
                base.OnMessage(message);
            }
        }

        void RegisterTurtles(Dictionary<uint, TurtleData> data)
        {
            foreach(uint netId in data.Keys)
            {
                var turtleObj = ClientScene.FindLocalObject(new NetworkInstanceId(netId));
                Turtle t = turtleObj.GetComponent<Turtle>();

                int role = data[netId].role;
                int index = data[netId].index;
                t.SetBasicData(role, index);

                // t.role = role;
                // t.index = index;

                RegisterInClient(t);
            }
        }

        public void RegisterInClient(Turtle turtle)
        {
            uint netId = turtle.GetComponent<NetworkIdentity>().netId.Value;

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

            // TODO offline case
            if(mode == Mode.OnlineMode)
            {
                if(netId > 0)
                {
                    if(turtlesByNetId.ContainsKey(netId))
                    {
                        Log.Error("Turtle with netId {0} already registered!", netId);
                        return;
                    }

                    turtlesByNetId.Add(netId, turtle);
                }
                else
                {
                    Log.Warn("netId is still zero");
                }
            }
        }

        ///// Utils
        
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

    } // class TurtleClient

} // namespace Turtle

