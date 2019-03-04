using System.Collections;

using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.Game;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        TBPlayer playingPlayer = null;

        public TurnBasedClient(Mode mode, DualServer server = null) : base(mode, server)
        {
            // noop
        }

        protected override void OnLateJoin(MessageStackMessage messageStack)
        {
            
            if(gameState == GameState.Playing)
            {
                var currentPlayerMessage = messageStack.ReadMessage<PlayerMessage>();
                var currentPlayerId = currentPlayerMessage.playerId;

                if(currentPlayerId > 0)
                {
                    playingPlayer = connections.GetPlayerAs<TBPlayer>(currentPlayerId);

                    if(playingPlayer == null)
                    {
                        Log.Error("Current player not found");
                        return;
                    }

                    playingPlayer.SetPlaying(true);
                }
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
            // noop
        }

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.StartTurn:

                    var turnMsg = message.ReadInternalMessage<PlayerMessage>();

                    var playerId = turnMsg.playerId;

                    if(playerId == 0)
                    {
                        Log.Error("Unexpected StartTurn message with playerId=0");
                        return;
                    }

                    var player = connections.GetPlayerIfAny(playerId);

                    if(player == null)
                    {
                        Log.Error("Playing player not found id={0}", playerId);
                        return;
                    }

                    IsMyTurn(connections.GetPlayerAs<TBPlayer>(player));

                    break;

                case MsgType.EndTurn:
                    if(playingPlayer == null)
                    {
                        Log.Warn("Already cleaned up");
                        return;
                    }

                    playingPlayer.SetPlaying(false);
                    playingPlayer = null;

                    break;

                default:
                    base.OnMessage(message);
                    break;
            }
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
                DualNetworkManager.instance.StartCoroutine(PlayTurn());
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
            SendToServer(MsgType.EndTurn, new EmptyMessage());
        }

        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();
        protected abstract void OnEndTurn(TBPlayer player);

        /*
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
                /*
                foreach(var p in DualNetworkManager.instance.OfflinePlayers())
                {
                    var pp = (OfflinePlayer)p;
                    var tbp = pp.GetComponent<TBPlayer>();
                    players.Add(p.GetId(), tbp);
                }
                * /
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
        */
    } // class TurnBasedClient

} // namespace Julo.TurnBased
