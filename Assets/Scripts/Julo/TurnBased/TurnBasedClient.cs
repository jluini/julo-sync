using System.Collections;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;
using Julo.Game;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        TurnBasedServer turnBasedServer;
        TurnBasedContext clientContext;

        public TurnBasedContext turnBasedContext
        {
            get
            {
                if(isHosted)
                {
                    return turnBasedServer.turnBasedContext;
                }
                else
                {
                    return clientContext;
                }
            }
        }

        public TurnBasedClient(Mode mode, DualServer server, DualPlayer playerModel) : base(mode, server, playerModel)
        {
            if(isHosted)
            {
                turnBasedServer = (TurnBasedServer)server;
            }
            else
            {
                clientContext = new TurnBasedContext();
            }
        }

        protected override void OnLobbyJoin(ListOfMessages listOfMessages)
        {
            //
        }

        protected override void OnLateJoin(ListOfMessages listOfMessages)
        {
            if(gameContext.gameState == GameState.Playing)
            {
                var playerMessage = listOfMessages.ReadMessage<DualPlayerSnapshot>();

                var player = (TurnBasedPlayer)dualContext.GetPlayer(playerMessage);

                if(player != null)
                {
                    turnBasedContext.currentPlayer = player;
                    turnBasedContext.currentPlayer.SetPlaying(true);
                }
                else
                {
                    // nobody is playing
                }
            }
        }
        
        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.StartTurn:

                    var turnMsg = message.ReadInternalMessage<DualPlayerSnapshot>();
                    var turnPlayer = (TurnBasedPlayer)dualContext.GetPlayer(turnMsg);

                    StartTurn(turnPlayer);

                    break;

                case MsgType.EndTurn:

                    if(!isHosted)
                    {
                        if(turnBasedContext.currentPlayer == null)
                        {
                            Log.Warn("Already cleaned up");
                            return;
                        }
                        turnBasedContext.currentPlayer.SetPlaying(false);
                        turnBasedContext.currentPlayer = null;
                    }

                    break;

                default:
                    base.OnMessage(message);
                    break;
            }
        }

        void StartTurn(TurnBasedPlayer player)
        {
            if(!isHosted)
            {
                if(turnBasedContext.currentPlayer != null)
                {
                    Log.Error("A player is already playing here!");
                    turnBasedContext.currentPlayer.SetPlaying(false);
                }

                turnBasedContext.currentPlayer = player;
                turnBasedContext.currentPlayer.SetPlaying(true);
            }

            if(player.IsLocal())
            {
                DualNetworkManager.instance.StartCoroutine(PlayTurn());
            }
        }

        bool turnEndedByServer;

        // only in client that owns the current player
        IEnumerator PlayTurn()
        {
            OnStartLocalTurn(turnBasedContext.currentPlayer);

            turnEndedByServer = false;

            do
            {
                yield return new WaitForEndOfFrame();

                if(turnEndedByServer)
                {
                    yield break;
                }

            } while(TurnIsOn());

            OnEndLocalTurn(turnBasedContext.currentPlayer);
            
            SendToServer(MsgType.EndTurn, new EmptyMessage());

            yield break;
        }

        protected abstract void OnStartLocalTurn(TurnBasedPlayer player);
        protected abstract bool TurnIsOn();
        protected abstract void OnEndLocalTurn(TurnBasedPlayer player);

    } // class TurnBasedClient

} // namespace Julo.TurnBased
