using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        public static TurnBasedClient instance = null;

        public int FrameStep = 5;

        ClientPlayers<TBPlayer> clientPlayers;

        TBPlayer playingPlayer = null;

        public override void OnStartClient()
        {
            instance = this;

            Log.Debug("Initating TBClient({0})", mode);
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
        
        // TODO call this!!

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
        
        public void TurnIsOver()
        {
            if(playingPlayer == null)
            {
                Log.Warn("Already cleaned up");
            }

            playingPlayer.SetPlaying(false);
            playingPlayer = null;
        }
        
        // only in client that owns the current player
        IEnumerator PlayTurn()
        {
            OnStartTurn(playingPlayer);

            int frameNumber = 0;
            do
            {
                frameNumber++;
                if(frameNumber % FrameStep == 0)
                {
                    SendToServer(Julo.TurnBased.MsgType.GameState, GetStateMessage());
                }

                yield return new WaitForEndOfFrame();

            } while(TurnIsOn());

            SendToServer(Julo.TurnBased.MsgType.GameState, GetStateMessage());

            //playingPlayer.TurnIsOverCommand();
            SendToServer(Julo.TurnBased.MsgType.EndTurn, new EmptyMessage());
        }

        public override void OnMessage(WrappedMessage message)
        {
            short msgType = message.messageType;

            if(msgType == Julo.TurnBased.MsgType.StartTurn)
            {
                var turnMsg = message.ReadExtraMessage<TurnMessage>();
                var netId = turnMsg.playerNetId;
                Log.Debug("Recibí StartTurn({0})", netId);

                var player = clientPlayers.GetPlayerByNetId(netId);

                IsMyTurn(player);
            }
            else if(msgType == Julo.TurnBased.MsgType.EndTurn)
            {
                // TODO check!

                TurnIsOver();
            }
            else
            {
                base.OnMessage(message);
            }
        }
        
        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();
        public abstract MessageBase GetStateMessage();

    } // class TurnBasedClient

} // namespace Julo.TurnBased
