using System.Collections.Generic;

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

        // only remote client
        Dictionary<uint, GamePlayerMessage> pendingPlayers = new Dictionary<uint, GamePlayerMessage>();

        public GameClient(Mode mode, DualServer server = null) : base(mode, server)
        {
            this.gameState = GameState.Unknown;
            this.numRoles = 0;
            this.sceneName = "";
        }

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

        public override void ReadPlayer(DualPlayerMessage dualPlayer, MessageStackMessage messageStack)
        {
            base.ReadPlayer(dualPlayer, messageStack);

            var gamePlayerData = messageStack.ReadMessage<GamePlayerMessage>();

            pendingPlayers.Add(dualPlayer.netId, gamePlayerData);

            //messageStack.Add(new GamePlayerMessage(gamePlayer.role, gamePlayer.username));
        }

        public override void ResolvePlayer(OnlineDualPlayer player, DualPlayerMessage dualPlayerData)
        {
            base.ResolvePlayer(player, dualPlayerData);

            var netId = dualPlayerData.netId;

            if(player.NetworkId() != netId)
            {
                Log.Error("Not resolved! {0} != {1}", player.NetworkId(), netId);
                return;
            }

            if(!pendingPlayers.ContainsKey(netId))
            {
                Log.Error("Player {0} not registered", netId);
                return;
            }

            var gamePlayerMessage = pendingPlayers[netId];
            pendingPlayers.Remove(netId);

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

            Log.Debug("Resolved {0} to {1}:{2}", player.NetworkId(), role, username);
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