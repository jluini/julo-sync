﻿using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Network;
using Julo.Logging;

namespace Julo.TurnBased
{
    [RequireComponent(typeof(OnlinePlayer))]
    public class OnlineTBPlayer : NetworkBehaviour, TBPlayer
    {
        List<TBPlayerListener> listeners = new List<TBPlayerListener>();
        
        // TODO move this initializations to Start() 

        OnlinePlayer _dnmPlayer;
        OnlinePlayer dnmPlayer
        {
            get
            {
                if(_dnmPlayer == null)
                {
                    _dnmPlayer = GetComponent<OnlinePlayer>();

                    if(_dnmPlayer == null)
                    {
                        Log.Error("Component OnlinePlayer not found!");
                    }
                }

                return _dnmPlayer;
            }
        }

        /*
        public int GetConnection()
        {
            if(NetworkServer.active)
            {
                return connectionToClient.connectionId;
            }
            throw new System.Exception("Invalid call of GetConnection");
        }
        public short GetControllerId()
        {
            if(NetworkServer.active)
            {
                return playerControllerId;
            }
            throw new System.Exception("Invalid call of GetControllerId");
        }
        */
        /*
        // only server
        public void TurnIsStartedRpc()
        {
            RpcIsMyTurn();
        }
        [ClientRpc]
        void RpcIsMyTurn()
        {
            TurnBasedClient.instance.IsMyTurn(this, isLocalPlayer);
        }
        */
        
        public bool IsLocal()
        {
            if(dnmPlayer == null)
            {
                Log.Error("Invalid call of IsLocal");
                return false;
            }

            return dnmPlayer.IsLocal();
        }
        
        /*
        // only in client owning this player
        public void TurnIsOverCommand()
        {
            CmdMyTurnIsOver();
        }
        [Command]
        void CmdMyTurnIsOver()
        {
            TurnBasedServer.instance.MyTurnIsOver();

            RpcMyTurnIsOver();
        }
        */
        /*
        [ClientRpc]
        void RpcMyTurnIsOver()
        {
            TurnBasedClient.instance.TurnIsOver();
        }
        */

        // only in client owning this player 
        /*public void GameStateCommand()
        {
            // TODO singletons and references...
            var stateMessage = TurnBasedClient.instance.GetStateMessage();
            NetworkManager.singleton.client.Send(MsgType.GameState, stateMessage);
        }*/

        public uint GetId()
        {
            if(dnmPlayer == null)
            {
                Log.Error("Invalid call of GetId");
                return 10000;
            }

            return dnmPlayer.GetId();
        }

        public string GetName()
        {
            if(dnmPlayer == null)
            {
                Log.Error("Invalid call of GetName");
                return "-";
            }

            return dnmPlayer.GetName();
        }

        public int GetRole()
        {
            if(dnmPlayer == null)
            {
                Log.Error("Invalid call of GetRole");
                return 0;
            }

            return dnmPlayer.GetRole();
        }

        /////////////// Listening ///////////////

        public void AddListener(TBPlayerListener listener)
        {
            listeners.Add(listener);
        }

        public void SetPlaying(bool isPlaying)
        {
            foreach(TBPlayerListener l in listeners)
            {
                l.SetPlaying(isPlaying);
            }
        }

    } // class OfflineTBPlayer

} // namespace Julo.TurnBased;

