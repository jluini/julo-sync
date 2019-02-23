using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedServer : GameServer
    {
        // TODO use singleton?
        public static TurnBasedServer instance;

        public float preturnWaitTime = 1f;

        bool aPlayerIsPlaying = false;

        //protected Mode mode;
        //protected int numRoles;

        RoleData[] roleData;

        int lastRolePlayed = 0;

        protected override void OnStartServer()
        {
            instance = this;

            this.roleData = new RoleData[numRoles];

            for(int r = 0; r < numRoles; r++)
            {
                this.roleData[r] = new RoleData();
            }
        }

        public override void StartGame()
        {
            SpawnInitialUnits();
        }

        public void InitialUnitsWereSpawned()
        {
            StartCoroutine(GameRoutine());
        }

        IEnumerator GameRoutine()
        {
            // TODO try without this
            yield return new WaitForSecondsRealtime(.1f);
            
            var stateMessage = GetStateMessage();
            
            SendToAll(Julo.TurnBased.MsgType.InitialState, stateMessage);

            // TODO wait for confirmation!

            do
            {
                yield return new WaitForSecondsRealtime(preturnWaitTime);

                // next turn
                int someAliveRole = -1;
                int aliveRoles = 0;

                for(int r = 1; r <= numRoles; r++)
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
                        if(nextRoleToPlay > numRoles)
                        {
                            nextRoleToPlay = 1;
                        }

                        if(roleData[nextRoleToPlay - 1].isAlive)
                        {
                            break;
                        }
                    }

                    lastRolePlayed = nextRoleToPlay;

                    // it's turn for nextRoleToPlay

                    var players = GetPlayersForRole(nextRoleToPlay);

                    // TODO it's picking always the first player of the role
                    var nextPlayer = players[0];
                    var nextPlayerId = nextPlayer.GetId();

                    //SendTo(nextPlayer.GetConnection(), MsgType.ItsYourTurn, new 
                    //nextPlayer.TurnIsStartedRpc();

                    //Log.Debug("It's turn of {0} ({1})", nextRoleToPlay, nextPlayerId);

                    if(mode == Mode.OnlineMode)
                    {
                        SendToAll(MsgType.StartTurn, new TurnMessage(nextPlayerId));
                    }

                    aPlayerIsPlaying = true;

                    do
                    {

                        yield return new WaitForEndOfFrame();
                    
                    } while(aPlayerIsPlaying);
                }
            } while(true);

            // TODO
        }

        public void MyTurnIsOver()
        {
            // TODO check that the way is right

            aPlayerIsPlaying = false;
        }

        public override void OnMessage(WrappedMessage message, int from)
        {
            short msgType = message.messageType;
            if(msgType == MsgType.EndTurn)
            {
                // TODO check everytring!
                MyTurnIsOver();
                SendToAll(MsgType.EndTurn, new EmptyMessage());
            }
            else
            {
                base.OnMessage(message, from);
            }
        }

        //protected abstract void OnStartServer();
        protected abstract void SpawnInitialUnits();

        protected abstract bool RoleIsAlive(int numRole);
        //protected abstract void ApplyState(NetworkMessage stateMessage);

    } // class TurnBasedServer

} // namespace Julo.TurnBased

