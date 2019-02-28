using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public class GameClient : DualClient
    {

        GameState gameState;
        int numRoles;
        string sceneName;

        // creates hosted client
        public GameClient(Mode mode, DualServer server) : base(mode, server)
        {
            Log.Debug("Creating hosted GameClient: {0}", mode);

            this.gameState = GameState.NoGame;
            this.numRoles = 0;
            this.sceneName = "";
        }

        // creates remote client
        public GameClient(StartRemoteClientMessage startMessage) : base(startMessage)
        {
            Log.Debug("Creating non-hosted GameClient");

            var message = startMessage.ReadInitialMessage<GameStatusMessage>();

            gameState = message.state;
            numRoles = message.numRoles;
            sceneName = message.sceneName;

            switch(gameState)
            {
                case GameState.NoGame:
                    break;
                case GameState.Preparing:
                case GameState.Playing:
                case GameState.GameOver:

                    DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
                    {
                        // LATE JOINING

                        // TODO read actual game state
                    });

                    break;
            }
        }

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.StartGame:

                    var startGameMessage = message.ReadExtraMessage<StartGameMessage>();

                    numRoles = startGameMessage.numRoles;
                    sceneName = startGameMessage.sceneName;

                    CheckState(GameState.NoGame);

                    SetState(GameState.Preparing);

                    DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
                    {
                    });

                    SetState(GameState.Playing);

                    break;

                default:
                    base.OnMessage(message);
                    break;
            }
        }

        // Checking

        void CheckState(GameState expectedState)
        {
            if(gameState != expectedState)
            {
                WrongState(gameState.ToString());
            }
        }

        void CheckState(GameState[] expectedStates)
        {
            if(!System.Array.Exists<GameState>(expectedStates, s => s == gameState))
            {
                WrongState(gameState.ToString());
            }
        }

        void WrongState(string stateString)
        {
            var errorMessage = System.String.Format("GameClient: unexpected state '{0}'", stateString);
            Log.Warn(errorMessage);
            throw new System.Exception(errorMessage);
        }

        /// 

        void SetState(GameState newState)
        {
            // TODO mostrar cambio?
            this.gameState = newState;
        }


    } // class GameClient

} // namespace Julo.Game