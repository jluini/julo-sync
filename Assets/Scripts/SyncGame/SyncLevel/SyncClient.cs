using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace SyncGame
{
    public class SyncClient : TurnBasedClient
    {
        public static new SyncClient instance = null;

        public static float torque = 5f;

        public int frameStep = 15;

        Unit targetUnit = null;

        enum TurnType { None, Keyboard, Wait }

        TurnType currentTurn = TurnType.None;
        bool turnEnded = true;

        public delegate void GameStartedDelegate();
        GameStartedDelegate gameStartedDelegate;

        // only if hosted
        SyncServer syncServer;

        // only if remote
        Unit onlineUnitModel;
        SyncMatch remoteMatch;

        SyncMatch match
        {
            get
            {
                return isHosted ? syncServer.GetMatch() : remoteMatch;
            }
        }

        // starts hosted client
        public SyncClient(Mode mode, GameStartedDelegate gameStartedDelegate, DualServer server) : base(mode, server)
        {
            instance = this;
            this.gameStartedDelegate = gameStartedDelegate;

            if(server == null)
            {
                Log.Error("No server");
            }
            else
            {
                syncServer = (SyncServer)server;
            }
        }

        // starts remote client
        public SyncClient(GameStartedDelegate gameStartedDelegate, Unit onlineUnitModel) : base(Mode.OnlineMode, null)
        {
            instance = this;

            this.gameStartedDelegate = gameStartedDelegate;
            this.onlineUnitModel = onlineUnitModel;

            if(onlineUnitModel == null)
            {
                Log.Error("No unit model");
            }

            remoteMatch = new SyncMatch();
        }

        protected override void OnLateJoin(MessageStackMessage messageStack)
        {
            base.OnLateJoin(messageStack);

            if(gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                var stateMessage = messageStack.ReadMessage<SyncGameState>();
                remoteMatch.CreateFromInitialState(numRoles, onlineUnitModel, stateMessage);

                OnGameStarted();
            }
        }

        public override void ReadPlayer(DualPlayerMessage dualPlayerData, MessageStackMessage stack)
        {
            base.ReadPlayer(dualPlayerData, stack);

            // TODO ...
        }

        protected override void OnPlayerResolved(OnlineDualPlayer player, DualPlayerMessage playerScreenshot)
        {
            base.OnPlayerResolved(player, playerScreenshot);

            // noop
        }

        protected override void OnPrepareToStart(MessageStackMessage messageStack)
        {
            base.OnPrepareToStart(messageStack);

            var stateMessage = messageStack.ReadMessage<SyncGameState>();

            remoteMatch.CreateFromInitialState(numRoles, onlineUnitModel, stateMessage);
        }

        protected override void OnGameStarted()
        {
            base.OnGameStarted();

            gameStartedDelegate();
        }

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.ServerUpdate:
                    if(!isHosted)
                    {
                        var newState = message.ReadInternalMessage<SyncGameState>();
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

            var units = match.GetUnitsForRole(role);
            var aliveUnits = units.FindAll(t => !t.dead);

            if(aliveUnits.Count == 0)
            {
                Log.Warn("No alive units with role {0}", role);
                currentTurn = TurnType.Wait;
                turnEnded = false;
                DualNetworkManager.instance.StartCoroutine(EndTurnDelayed());
                return;
            }

            currentTurn = TurnType.Keyboard;

            targetUnit = GetNextUnit(aliveUnits);
            targetUnit.lastUse = DateTime.Now;
            targetUnit.SetPlaying(true);
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

            if(targetUnit.dead)
            {
                currentTurn = TurnType.None;
                return false;
            }

            float hor = Input.GetAxis("Horizontal");
            float fire3 = Input.GetAxis("Fire3");

            if(hor > 0)
            {
                targetUnit.AddTorque(-torque);
            }
            else if(hor < 0)
            {
                targetUnit.AddTorque(+torque);
            }

            if(fire3 > 0)
            {
                targetUnit.SetPlaying(false);
                currentTurn = TurnType.None;
                return false;
            }

            foreach(var t in match.GetAllUnits())
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
        
        Unit GetNextUnit(List<Unit> units)
        {
            // TODO criteria to pick unit
            if(units.Count > 0)
            {
                var t = units[0];
                for(int i = 1; i < units.Count; i++)
                {
                    var t2 = units[i];
                    if(t2.lastUse < t.lastUse)
                    {
                        t = t2;
                    }
                }

                return t;
            }
            Log.Error("No units");
            return null;
        }

    } // class SyncClient

} // namespace SyncGame
