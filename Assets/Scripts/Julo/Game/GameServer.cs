using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public enum GameState { NoGame, Preparing, Playing, GameOver }


    public class GameServer : DualServer
    {
        public static GameServer instance = null;

        public new GameClient localClient = null;
        public List<IDualPlayer>[] playersPerRole; // TODO should be GamePlayer's?

        public GameState gameState;
        public int numRoles;
        string sceneName;

        public GameServer(Mode mode) : base(mode)
        {
            instance = this;

            gameState = GameState.NoGame;
            numRoles = 2;
            sceneName = "beach";
        }

        // only online mode
        public override void WriteRemoteClientData(List<MessageBase> messages)
        {
            base.WriteRemoteClientData(messages);

            // TODO pass role data to clients?
            messages.Add(new GameStatusMessage(gameState, numRoles, sceneName));
            //Log.Debug("Written game status: {0}, {1}, {2}", gameState, numRoles, sceneName);
        }

        public void StartGame(int numRoles, List<IDualPlayer>[] playersPerRole, string sceneName)
        {
            Log.Debug("START GAME!!");

            this.numRoles = numRoles;
            this.playersPerRole = playersPerRole;
            this.sceneName = sceneName;

            SetState(GameState.Preparing);

            // TODO avoid singleton
            DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
            {
                SendToAll(MsgType.StartGame, new StartGameMessage(numRoles, sceneName));

                // waiting all clients to send OnReadyToStart
            });
        }

        ////////// Player //////////

        // server
        public override void OnPlayerAdded(IDualPlayer player)
        {
            base.OnPlayerAdded(player);

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);

            int role = 2; // TODO
            string username = "Jorge"; // TODO

            gamePlayer.SetRole(role);
            gamePlayer.SetUsername(username);
        }

        public override void WritePlayer(IDualPlayer player, List<MessageBase> messageStack)
        {
            base.WritePlayer(player, messageStack);

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);
            messageStack.Add(new GamePlayerMessage(gamePlayer.role, gamePlayer.username));
        }

        ////////// Roles //////////

        public void ChangeRole(GamePlayer player)
        {
            Log.Debug("Trying to change role!!");
            /*
            Log.Warn("ChangeRole not implemented");
            CheckState(new DNMState[] { DNMState.Offline, DNMState.Host });
            CheckGameState(GameState.NoGame);

            int currentRole = player.GetRole();

            int newRole;
            if(currentRole == DNM.SpecRole)
            {
                newRole = DNM.FirstPlayerRole;
            }
            else
            {
                newRole = currentRole + 1;
                if(newRole > levelData.MaxPlayers)
                {
                    // no spec role in offline mode
                    newRole = state == DNMState.Offline ? DNM.FirstPlayerRole : DNM.SpecRole;
                }
            }

            if(newRole != currentRole)
            {
                player.SetRole(newRole);
            }
            else
            {
                Log.Warn("No role to change?");
            }
            */
        }

        ////////// * //////////

        public void TryToStartGame()
        {
            Log.Warn("TryToStartGame not implemented");
        }

        ////////// Messaging //////////

        protected override void OnMessage(WrappedMessage message, int from)
        {
            base.OnMessage(message, from);
        }

        ///

        void SetState(GameState newState)
        {
            // TODO mostrar cambio?
            this.gameState = newState;
        }

    } // class GameServer

} // namespace Julo.Game