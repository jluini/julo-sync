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

            clientPlayers = new CacheClientPlayers<TBPlayer>();
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
        /*
        TBPlayer GetPlayerByNetId(uint netId)
        {
            if(!clientPlayers.ContainsKey(netId))
            {
                var p = ClientScene.FindLocalObject(new NetworkInstanceId(netId));
                if(p == null)
                {
                    Log.Error("No object");
                    return null;
                }
                var tbPlayer = p.GetComponent<TBPlayer>();
                if(tbPlayer == null)
                {
                    Log.Error("No TBPlayer");
                    return null;
                }
                clientPlayers.Add(netId, tbPlayer);
            }

            return clientPlayers[netId];
        }
        */
        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();
        public abstract MessageBase GetStateMessage();

    } // class TurnBasedClient

} // namespace Julo.TurnBased
