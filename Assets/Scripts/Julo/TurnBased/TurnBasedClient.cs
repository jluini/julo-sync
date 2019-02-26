using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        public static new TurnBasedClient instance = null;

        ClientPlayers<TBPlayer> clientPlayers;

        TBPlayer playingPlayer = null;

        // only local
        TurnBasedServer tbServer;


        // local case
        public override void OnStartLocalClient(GameServer server)
        {
            base.OnStartLocalClient(server);

            instance = this;
            this.tbServer = (TurnBasedServer)server;

            if(mode == Mode.OfflineMode)
            {
                var players = new Dictionary<uint, TBPlayer>();

                // TODO!!!
                foreach(var p in DualNetworkManager.instance.OfflinePlayers())
                {
                    var pp = (OfflinePlayer)p;
                    var tbp = pp.GetComponent<TBPlayer>();
                    players.Add(p.GetId(), tbp);
                }

                clientPlayers = new FixedClientPlayers<TBPlayer>(players);
            }
            else
            {
                clientPlayers = new CacheClientPlayers<TBPlayer>();
            }
        }
            
        // remote case
        public override void OnStartRemoteClient(StartGameMessage initialMessages)
        {
            base.OnStartRemoteClient(initialMessages);

            instance = this;

            clientPlayers = new CacheClientPlayers<TBPlayer>();
        }
        
        void IsMyTurn(TBPlayer player)
        {
            if(playingPlayer != null)
            {
                Log.Error("A player is already playing here!");
                return;
            }

            playingPlayer = player;
            playingPlayer.SetPlaying(true);

            if(player.IsLocal())
                StartCoroutine(PlayTurn());
        }
        
        // only in client that owns the current player
        IEnumerator PlayTurn()
        {
            OnStartTurn(playingPlayer);

            do
            {
                yield return new WaitForEndOfFrame();

            } while(TurnIsOn());

            OnEndTurn(playingPlayer);
            SendToServer(Julo.TurnBased.MsgType.EndTurn, new EmptyMessage());
        }

        ////// Message handlers

        void OnStartTurnMessage(TurnMessage turnMsg)
        {
            var netId = turnMsg.playerNetId;

            var player = clientPlayers.GetPlayerByNetId(netId);

            IsMyTurn(player);
        }

        void OnEndTurnMessage()
        {
            if(playingPlayer == null)
            {
                Log.Warn("Already cleaned up");
                return;
            }

            playingPlayer.SetPlaying(false);
            playingPlayer = null;
        }


        public override void OnMessage(WrappedMessage message)
        {
            short msgType = message.messageType;

            if(msgType == Julo.TurnBased.MsgType.StartTurn)
            {
                var turnMsg = message.ReadExtraMessage<TurnMessage>();
                OnStartTurnMessage(turnMsg);
            }
            else if(msgType == Julo.TurnBased.MsgType.EndTurn)
            {
                OnEndTurnMessage();
            }
            else
            {
                base.OnMessage(message);
            }
        }
        
        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();
        protected abstract void OnEndTurn(TBPlayer player);

    } // class TurnBasedClient

} // namespace Julo.TurnBased
