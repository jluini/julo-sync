using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedServer : GameServer
    {
        // TODO use singleton?
        public static TurnBasedServer instance;

        bool aPlayerIsPlaying = false;

        protected Mode mode;
        protected int numRoles;

        RoleData[] roleData;

        int lastRolePlayed = -1;

        public override void StartServer(Mode mode, int numRoles, List<Player>[] playersPerRole)
        {
            instance = this;
            Log.Debug("Starting turn based game");

            // TODO check players and params

            this.mode = mode;
            this.numRoles = numRoles;

            this.roleData = new RoleData[numRoles];

            for(int r = 0; r < numRoles; r++)
            {
                List<TBPlayer> tbPlayers = new List<TBPlayer>();

                foreach(Player p in playersPerRole[r])
                {
                    TBPlayer tbPlayer;
                    if(mode == Mode.OfflineMode)
                    {
                        OfflinePlayer pp = (OfflinePlayer)p;

                        tbPlayer = pp.GetComponent<OfflineTBPlayer>();
                    }
                    else
                    {
                        OnlinePlayer pp = (OnlinePlayer)p;

                        tbPlayer = pp.GetComponent<OnlineTBPlayer>();
                    }

                    if(tbPlayer != null)
                    {
                        tbPlayers.Add(tbPlayer);
                    }
                    else
                    {
                        Log.Error("Invalid player setup!!");
                    }
                }

                this.roleData[r] = new RoleData(tbPlayers);
            }

            OnStartGame();

            StartCoroutine(StartGameDelayed());
        }

        IEnumerator StartGameDelayed()
        {
            yield return new WaitForSecondsRealtime(1f);

            do
            {
                yield return new WaitForSecondsRealtime(1f);

                // next turn
                int someAliveRole = -1;
                int aliveRoles = 0;

                for(int r = 0; r < numRoles; r++)
                {
                    bool roleWasAlive = roleData[r].isAlive;
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
                        roleData[r].isAlive = roleIsAlive;
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
                        if(nextRoleToPlay >= numRoles)
                        {
                            nextRoleToPlay = 0;
                        }

                        if(roleData[nextRoleToPlay].isAlive)
                        {
                            break;
                        }
                    }

                    lastRolePlayed = nextRoleToPlay;

                    // it's turn for nextRoleToPlay
                    // TODO it's picking always the first player of the role
                    roleData[nextRoleToPlay].players[0].TurnIsStartedRpc();

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

        public abstract void OnStartGame();
        public abstract bool RoleIsAlive(int numRole);

    } // class TurnBasedServer

} // namespace Julo.TurnBased

