using System.Collections;

using UnityEngine;

using Julo.Logging;
using Julo.Network;

namespace Julo.TurnBased
{
    public abstract class TurnBasedClient : GameClient
    {
        // TODO use singleton?
        public static TurnBasedClient instance = null;

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

            Log.Debug("Starting TBClient ({0}, {1}, {2})", mode, isHosted, numRoles);

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

        IEnumerator PlayTurn()
        {
            OnStartTurn(playingPlayer);

            do
            {
                yield return new WaitForEndOfFrame();
            } while(TurnIsOn());

            playingPlayer.TurnIsOverCommand();
        }

        public abstract void OnStartClient();
        protected abstract void OnStartTurn(TBPlayer player);
        protected abstract bool TurnIsOn();

    } // class TurnBasedClient

} // namespace Julo.TurnBased

