using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.Game;

namespace Julo.TurnBased
{
    public enum TurnBasedState { NoGame, Playing, GameOver }

    public abstract class TurnBasedServer : GameServer
    {
        public new static TurnBasedServer instance;

        TurnBasedState state = TurnBasedState.NoGame;


        float preturnWaitTime = 1f;
        RoleData[] roleData;
        int lastRolePlayed = 0;
        TurnBasedPlayer playingPlayer = null;

        public TurnBasedServer(Mode mode, DualPlayer playerModel) : base(mode, playerModel)
        {
            instance = this;
        }

        // only online mode
        public override void WriteRemoteClientData(ListOfMessages listOfMessages)
        {
            base.WriteRemoteClientData(listOfMessages);

            if(gameContext.gameState == GameState.Playing)
            {
                listOfMessages.Add(new DualPlayerSnapshot(playingPlayer));
            }
        }

        ////////// Player //////////


        ////////// Messaging //////////

        protected override void OnMessage(WrappedMessage message, int from)
        {
            switch(message.messageType)
            {
                case MsgType.EndTurn:
                    EndTurn();
                    break;

                default:
                    base.OnMessage(message, from);
                    break;
            }
        }

        protected override void OnPrepareToStart(ListOfMessages listOfMessages)
        {
            roleData = new RoleData[gameContext.numRoles];
            for(int r = 1; r <= gameContext.numRoles; r++)
            {
                roleData[r - 1] = new RoleData();
            }
        }

        protected override void OnStartGame()
        {
            state = TurnBasedState.Playing;
            DualNetworkManager.instance.StartCoroutine(GameRoutine());
        }

        IEnumerator GameRoutine()
        {
            do
            {
                yield return new WaitForSecondsRealtime(preturnWaitTime);

                // next turn
                int someAliveRole = -1;
                int aliveRoles = 0;

                for(int r = 1; r <= gameContext.numRoles; r++)
                {
                    bool roleWasAlive = roleData[r - 1].isAlive;
                    bool roleIsAlive = RoleIsAlive(r);

                    if(roleIsAlive)
                    {
                        if(!roleWasAlive)
                        {
                            Log.Warn("Role {0} revived!", r);
                        }
                        aliveRoles++;
                        someAliveRole = r;
                    }

                    if(roleWasAlive != roleIsAlive)
                    {
                        roleData[r - 1].isAlive = roleIsAlive;
                    }
                }

                if(aliveRoles == 0)
                {
                    // it's a draw

                    Log.Debug("It's a draw");

                    state = TurnBasedState.GameOver;

                    // TODO
                    break;
                }
                else if(aliveRoles == 1)
                {
                    //it's a win for "someAliveRole"

                    Log.Debug("It's a win for {0}", someAliveRole);

                    state = TurnBasedState.GameOver;

                    // TODO
                    break;
                }
                else
                {
                    var matchFailed = false;

                    for(int r = 1; r <= gameContext.numRoles; r++)
                    {
                        if(!roleData[r - 1].isAlive)
                        {
                            continue;
                        }
                        var n = NumberOfPlayingPlayersForRole(r);
                        if(n < 1)
                        {
                            Log.Debug("There are {0} players for role {1}", n, r);
                            matchFailed = true;
                        }
                    }
                    
                    if(matchFailed)
                    {
                        Log.Debug("Game aborted");
                        state = TurnBasedState.GameOver;
                        break;
                    }

                    int nextRoleToPlay = lastRolePlayed;
                    while(true)
                    {
                        nextRoleToPlay++;
                        if(nextRoleToPlay > gameContext.numRoles)
                        {
                            nextRoleToPlay = 1;
                        }

                        if(roleData[nextRoleToPlay - 1].isAlive)
                        {
                            break;
                        }
                    }

                    lastRolePlayed = nextRoleToPlay;

                    OnStartTurn(nextRoleToPlay);

                    // it's turn for nextRoleToPlay
                    
                    var players = GetPlayingPlayersForRole(nextRoleToPlay);

                    playingPlayer = GetNextPlayer(players);

                    SendToAll(MsgType.StartTurn, new DualPlayerSnapshot(playingPlayer));
                    
                    do
                    {
                        yield return new WaitForEndOfFrame();
                    } while(playingPlayer != null);
                }
            } while(true);
        }

        void EndTurn()
        {
            SendToAll(MsgType.EndTurn, new EmptyMessage()); // TODO Send to all but!
            playingPlayer = null;
        }

        /// 

        protected new List<TurnBasedPlayer> GetPlayingPlayersForRole(int role)
        {
            var ret = new List<TurnBasedPlayer>();
            
            foreach(var gamePlayer in base.GetPlayingPlayersForRole(role))
            {
                ret.Add((TurnBasedPlayer)gamePlayer);
            }

            return ret;
        }

        TurnBasedPlayer GetNextPlayer(List<TurnBasedPlayer> players)
        {
            if(players.Count > 0)
            {
                var t = players[0];
                for(int i = 1; i < players.Count; i++)
                {
                    var t2 = players[i];
                    if(t2.lastUse < t.lastUse)
                    {
                        t = t2;
                    }
                }

                t.lastUse = DateTime.Now;

                return t;
            }
            Log.Error("No players");
            return null;
        }

        protected abstract void OnStartTurn(int role);
        protected abstract bool RoleIsAlive(int numRole);

        protected override void OnPlayerDisconnected(GamePlayer player)
        {
            base.OnPlayerDisconnected(player);

            var tbPlayer = (TurnBasedPlayer)player;

            if(state != TurnBasedState.Playing)
            {
                Log.Error("OnPlayerDisconnected but state = {0}", state);
                return;
            }

            if(playingPlayer == tbPlayer)
            {
                Log.Debug("Se desconectó el que estaba jugando!");
                EndTurn();
            }
        }

        protected override void OnPlayerRemoved(DualPlayer player)
        {
            base.OnPlayerRemoved(player);
        }

    } // class TurnBasedServer

} // namespace Julo.TurnBased

