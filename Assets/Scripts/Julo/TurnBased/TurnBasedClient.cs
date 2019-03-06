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

        public TurnBasedClient(Mode mode, DualServer server, DualPlayer playerModel) : base(mode, server, playerModel)
        {
            // noop
        }

        protected override void OnLateJoin(MessageStackMessage messageStack)
        {
            if(gameState == GameState.Playing)
            {
                var currentPlayerMessage = messageStack.ReadMessage<PlayerMessage>();

                var currentPlayerConnId = currentPlayerMessage.connectionId;
                var currentPlayerContId = currentPlayerMessage.controllerId;

                if(currentPlayerConnId >= 0)
                {
                    // TODO cast or cache TBPlayers?
                    
                    playingPlayer = (TBPlayer)dualContext.GetPlayer(currentPlayerConnId, currentPlayerContId);

                    if(playingPlayer == null)
                    {
                        Log.Error("Current player not found");
                        return;
                    }

                    playingPlayer.SetPlaying(true);
                }
                else
                {
                    // nobody is playing
                }
            }
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

                    var connId = turnMsg.connectionId;
                    var controllerId = turnMsg.controllerId;

                    // TODO cache players?

                    // TODO cast or cache TBPlayer?
                    var turnPlayer = (TBPlayer)dualContext.GetPlayer(connId, controllerId);

                    IsMyTurn(turnPlayer);

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

    } // class TurnBasedClient

} // namespace Julo.TurnBased
