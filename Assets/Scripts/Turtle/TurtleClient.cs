using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.TurnBased;

namespace Turtle
{
    public class TurtleClient : TurnBasedClient
    {

        // creates hosted client
        public TurtleClient(Mode mode, DualServer server) : base(mode, server)
        {
            // noop
        }

        // creates remote client
        public TurtleClient() : base() { }
        public override void InitializeState(MessageStackMessage startMessage)
        {
            base.InitializeState(startMessage);

            // TODO ...
        }

        public override void OnPlayerResolved(OnlineDualPlayer player, MessageStackMessage message)
        {
            base.OnPlayerResolved(player, message);

            // TODO ...
        }

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                default:
                    base.OnMessage(message);
                    break;
            }
        }

        /*
        public static new TurtleClient instance = null;

        public Turtle onlineTurtlePrefab;

        public static float torque = 5f;

        public int frameStep = 15;

        Turtle targetTurtle = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType tt = TurnType.None;
        bool turnEnded = true;

        TurtleMatch match
        {
            get
            {
                return isHosted ? turtleServer.GetMatch() : remoteMatch;
            }
        }

        // only if hosted
        TurtleServer turtleServer;

        // only if remote
        TurtleMatch remoteMatch;
        
        // local case
        public override void OnStartLocalClient(GameServer server)
        {
            base.OnStartLocalClient(server);

            instance = this;

            this.turtleServer = (TurtleServer) server;
        }

        // remote case
        public override void OnStartRemoteClient(StartGameMessage initialMessages)
        {
            base.OnStartRemoteClient(initialMessages);

            instance = this;

            var initialState = initialMessages.ReadInitialMessage<GameState>();

            remoteMatch = new TurtleMatch();
            remoteMatch.CreateFromInitialState(numRoles, onlineTurtlePrefab, initialState);
        }

        protected override void OnStartTurn(TBPlayer player)
        {
            if(tt != TurnType.None)
            {
                Log.Warn("Unexpected TurnType {0} (A)", tt);
            }

            int role = player.GetRole();

            var turtles = Match.GetTurtlesForRole(role);
            var aliveTurtles = turtles.FindAll(t => !t.dead);

            if(/*turtles == null || * / aliveTurtles.Count == 0)
            {
                Log.Warn("No alive turtles with role {0}", role);
                tt = TurnType.Wait;
                turnEnded = false;
                StartCoroutine(EndTurnDelayed());
                return;
            }

            tt = TurnType.Keyboard;

            targetTurtle = GetNextTurtle(aliveTurtles);

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

            if(Time.frameCount % FrameStep == 0)
            {
                SendToServer(MsgType.ClientUpdate, Match.GetState());
            }

            if(targetTurtle.dead)
            {
                tt = TurnType.None;
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

            foreach(var t in Match.GetAllTurtles())
            {
                t.SetDead(t.transform.position.y < -1.5f);
            }

            return true;
        }

        protected override void OnEndTurn(TBPlayer player)
        {
            SendToServer(MsgType.ClientUpdate, match.GetState());
        }

        IEnumerator EndTurnDelayed()
        {
            yield return new WaitForSecondsRealtime(2f);
            turnEnded = true;
        }
        
        public override void OnMessage(WrappedMessage message)
        {
            short msgType = message.messageType;

            if(msgType == MsgType.ServerUpdate)
            {
                if(!isHosted)
                {
                    var newState = message.ReadExtraMessage<GameState>();
                    Match.UpdateState(newState);
                }
            }
            else
            {
                base.OnMessage(message);
            }
        }

        ///// Utils
        
        Turtle GetNextTurtle(List<Turtle> turtles)
        {
            // TODO criteria to pick turtle
            if(turtles.Count > 0)
            {
                var t = turtles[0];
                for(int i = 1; i < turtles.Count; i++)
                {
                    var t2 = turtles[i];
                    if(t2.lastUse < t.lastUse)
                    {
                        t = t2;
                    }
                }

                return t;
            }
            Log.Error("No turtles");
            return null;
        }
        */
    } // class TurtleClient

} // namespace Turtle

