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
    public abstract class TurnBasedServer : GameServer
    {
        public new static TurnBasedServer instance;
        
        
        // only server
        float preturnWaitTime = 1f;
        RoleData[] roleData;
        int lastRolePlayed = 0;
        TBPlayer playingPlayer = null;

        // List<TBPlayer>[] playersPerRole;

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
                    SendToAll(MsgType.EndTurn, new EmptyMessage()); // TODO Send to all but!
                    playingPlayer = null;

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

                    // TODO
                    break;
                }
                else if(aliveRoles == 1)
                {
                    //it's a win for "someAliveRole"

                    Log.Debug("It's a win for {0}", someAliveRole);

                    // TODO
                    break;
                }
                else
                {
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
                    
                    // TODO we are casting here; cache instead?
                    var players = GetPlayersForRole(nextRoleToPlay);

                    playingPlayer = GetNextPlayer(players);

                    playingPlayer.lastUse = DateTime.Now;

                    SendToAll(MsgType.StartTurn, new DualPlayerSnapshot(playingPlayer));
                    
                    do
                    {
                        yield return new WaitForEndOfFrame();
                    } while(playingPlayer != null);
                }
            } while(true);
        }

        protected new List<TBPlayer> GetPlayersForRole(int role)
        {
            var ret = new List<TBPlayer>();
            
            foreach(var gamePlayer in base.GetPlayersForRole(role))
            {
                ret.Add((TBPlayer)gamePlayer);
            }

            return ret;
        }

        TBPlayer GetNextPlayer(List<TBPlayer> players)
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

                return t;
            }
            Log.Error("No players");
            return null;
        }

        protected abstract void OnStartTurn(int role);
        protected abstract bool RoleIsAlive(int numRole);

    } // class TurnBasedServer

} // namespace Julo.TurnBased

