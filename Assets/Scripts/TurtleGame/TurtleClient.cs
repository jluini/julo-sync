using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace TurtleGame
{
    public class TurtleClient : TurnBasedClient
    {
        public static new TurtleClient instance = null;

        public static float torque = 5f;

        public int frameStep = 15;

        Turtle targetTurtle = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType currentTurn = TurnType.None;
        bool turnEnded = true;

        public delegate void GameStartedDelegate();
        GameStartedDelegate gameStartedDelegate;

        // only if hosted
        TurtleServer turtleServer;

        // only if remote
        Turtle onlineTurtleModel;
        TurtleMatch remoteMatch;

        TurtleMatch match
        {
            get
            {
                return isHosted ? turtleServer.GetMatch() : remoteMatch;
            }
        }

        // starts hosted client
        public TurtleClient(Mode mode, GameStartedDelegate gameStartedDelegate, DualServer server) : base(mode, server)
        {
            instance = this;
            this.gameStartedDelegate = gameStartedDelegate;

            if(server == null)
            {
                Log.Error("No server");
            }
            else
            {
                turtleServer = (TurtleServer)server;
            }
        }

        // starts remote client
        public TurtleClient(GameStartedDelegate gameStartedDelegate, Turtle onlineTurtleModel) : base(Mode.OnlineMode, null)
        {
            instance = this;

            this.gameStartedDelegate = gameStartedDelegate;
            this.onlineTurtleModel = onlineTurtleModel;

            if(onlineTurtleModel == null)
            {
                Log.Error("No turtle model");
            }

            remoteMatch = new TurtleMatch();
        }

        protected override void OnLateJoin(MessageStackMessage messageStack)
        {
            base.OnLateJoin(messageStack);

            if(gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                var stateMessage = messageStack.ReadMessage<TurtleGameState>();
                remoteMatch.CreateFromInitialState(numRoles, onlineTurtleModel, stateMessage);

                OnGameStarted();
            }
        }

        public override void ReadPlayer(DualPlayerMessage dualPlayerData, MessageStackMessage stack)
        {
            base.ReadPlayer(dualPlayerData, stack);

            // TODO ...
        }

        public override void ResolvePlayer(OnlineDualPlayer player, DualPlayerMessage dualPlayerData)
        {
            base.ResolvePlayer(player, dualPlayerData);

            // TODO ...
        }

        protected override void OnPrepareToStart(MessageStackMessage messageStack)
        {
            base.OnPrepareToStart(messageStack);

            var stateMessage = messageStack.ReadMessage<TurtleGameState>();

            remoteMatch.CreateFromInitialState(numRoles, onlineTurtleModel, stateMessage);
        }

        protected override void OnGameStarted()
        {
            gameStartedDelegate();
        }

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.ServerUpdate:
                    if(!isHosted)
                    {
                        var newState = message.ReadInternalMessage<TurtleGameState>();
                        match.UpdateState(newState);
                    }

                    break;

                default:
                    base.OnMessage(message);
                    break;
            }
        }

        protected override void OnStartTurn(TBPlayer player)
        {
            if(currentTurn != TurnType.None)
            {
                Log.Warn("Unexpected TurnType {0} (A)", currentTurn);
            }

            int role = player.GetRole();

            var turtles = match.GetTurtlesForRole(role);
            var aliveTurtles = turtles.FindAll(t => !t.dead);

            if(aliveTurtles.Count == 0)
            {
                Log.Warn("No alive turtles with role {0}", role);
                currentTurn = TurnType.Wait;
                turnEnded = false;
                DualNetworkManager.instance.StartCoroutine(EndTurnDelayed());
                return;
            }

            currentTurn = TurnType.Keyboard;

            targetTurtle = GetNextTurtle(aliveTurtles);

            targetTurtle.SetPlaying(true);
        }

        protected override bool TurnIsOn()
        {
            if(currentTurn == TurnType.Wait)
            {
                if(turnEnded)
                {
                    currentTurn = TurnType.None;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else if(currentTurn != TurnType.Keyboard)
            {
                Log.Warn("Unexpected TurnType {0} (A)", currentTurn);
                return false;
            }

            if(Time.frameCount % frameStep == 0)
            {
                SendToServer(MsgType.ClientUpdate, match.GetState());
            }

            if(targetTurtle.dead)
            {
                currentTurn = TurnType.None;
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
                currentTurn = TurnType.None;
                return false;
            }

            foreach(var t in match.GetAllTurtles())
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

    } // class TurtleClient

} // namespace Turtle
