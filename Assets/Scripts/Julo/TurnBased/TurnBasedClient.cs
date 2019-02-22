using System.Collections;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        // TODO use singleton?
        public static TurnBasedClient instance = null;

        public int FrameStep = 5;

        protected Mode mode;
        protected bool isHosted;
        protected int numRoles;

        TBPlayer playingPlayer = null;

        public override void StartClient(Mode mode, bool isHosted, int numRoles)
        {
            instance = this;

            this.mode = mode;
            this.isHosted = isHosted;
            this.numRoles = numRoles;

            OnStartClient();
        }

        public void IsMyTurn(TBPlayer player, bool localToHere)
        {

            if(playingPlayer != null)
            {
                Log.Error("A player is already playing here!");
                return;
            }

            playingPlayer = player;
            playingPlayer.SetPlaying(true);

            if(localToHere)
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

            playingPlayer.TurnIsOverCommand();
        }

        public override void OnMessage(WrappedMessage msg)
        {
            base.OnMessage(msg);
        }

        public abstract void OnStartClient();
        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();
        public abstract MessageBase GetStateMessage();

    } // class TurnBasedClient

} // namespace Julo.TurnBased
