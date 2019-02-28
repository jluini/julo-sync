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

        public static TurnBasedServer instance;

        /*
        public float preturnWaitTime = 1f;
        bool aPlayerIsPlaying = false;
        RoleData[] roleData;
        int lastRolePlayed = 0;
        */

        public TurnBasedServer(Mode mode, CreateHostedClientDelegate clientDelegate = null) : base(mode, clientDelegate)
        {
            instance = this;
            Log.Debug("Creating TBServer");
        }

        // only online mode
        public override void WriteRemoteClientData(List<MessageBase> messages)
        {
            base.WriteRemoteClientData(messages);

            // TODO pass data to TurnBasedClient

            messages.Add(new UnityEngine.Networking.NetworkSystem.StringMessage("Nivel TB!!"));
        }

        protected override void OnMessage(WrappedMessage message, int from)
        {
            base.OnMessage(message, from);
        }

        /*
        protected override void OnStartServer()
        {
            base.OnStartServer();

            instance = this;

            this.roleData = new RoleData[numRoles];

            for(int r = 0; r < numRoles; r++)
            {
                this.roleData[r] = new RoleData();
            }
        }

        // only online mode
        public override void WriteInitialData(List<MessageBase> messages)
        {
            base.WriteInitialData(messages);
        }

        public override void StartGame()
        {
            StartCoroutine(GameRoutine());
        }
        */
        /*
        IEnumerator GameRoutine()
        {
            OnStartGame();

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

                    OnStartTurn(nextRoleToPlay);

                    // it's turn for nextRoleToPlay

                    var players = GetPlayersForRole(nextRoleToPlay);

                    // TODO it's picking always the first player of the role
                    var nextPlayer = players[0];
                    var nextPlayerId = nextPlayer.GetId();

                    SendToAll(MsgType.StartTurn, new TurnMessage(nextPlayerId));

                    aPlayerIsPlaying = true;

                    do
                    {
                        yield return new WaitForEndOfFrame();
                    
                    } while(aPlayerIsPlaying);
                }
            } while(true);
        }

        public void OnEndTurnMessage()
        {
            aPlayerIsPlaying = false;
            SendToAll(MsgType.EndTurn, new EmptyMessage()); // TODO Send to all but!
        }

        public override void OnMessage(WrappedMessage message, int from)
        {
            short msgType = message.messageType;
            if(msgType == MsgType.EndTurn)
            {
                OnEndTurnMessage();
            }
            else
            {
                base.OnMessage(message, from);
            }
        }

        protected abstract void OnStartGame();
        protected abstract void OnStartTurn(int role);

        protected abstract bool RoleIsAlive(int numRole);
        */
    } // class TurnBasedServer

} // namespace Julo.TurnBased

