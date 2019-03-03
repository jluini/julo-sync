using UnityEngine;

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
            this.gameState = GameState.NoGame;
            this.numRoles = 0;
            this.sceneName = "";
        }

        // creates remote client
        public GameClient() : base() { }

        public override void InitializeState(MessageStackMessage startMessage)
        {
            base.InitializeState(startMessage);
            var message = startMessage.ReadMessage<GameStatusMessage>();

            gameState = message.state;
            numRoles = message.numRoles;
            sceneName = message.sceneName;

            /*
            switch(gameState)
            {
                case GameState.NoGame:
                    Log.Debug("I joined but no game yet");
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
            */
        }

        public override void OnPlayerResolved(OnlineDualPlayer player, MessageStackMessage messageStack)
        {
            base.OnPlayerResolved(player, messageStack);

            var gamePlayerMessage = messageStack.ReadMessage<GamePlayerMessage>();

            int role = gamePlayerMessage.role;
            string username = gamePlayerMessage.username;

            //var b = (MonoBehaviour)player;
            //var gamePlayer = b.GetComponent<GamePlayer>();
            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);

            if(gamePlayer.role == role)
                Log.Warn("Role already set to {0}", role);

            if(gamePlayer.username == username)
                Log.Warn("Username already set to {0}", username);

            gamePlayer.SetRole(role);
            gamePlayer.SetUsername(username);
            //SetRole(role);

            Log.Debug("Resolved {0} to {1},{2}", player.NetworkId(), role, username);
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