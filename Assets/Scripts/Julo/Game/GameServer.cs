using System.Collections.Generic;

using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public enum GameState { NoGame, Preparing, Playing, GameOver }


    public class GameServer : DualServer
    {

        public new GameClient localClient = null;
        public List<IDualPlayer>[] playersPerRole; // TODO should be GamePlayer's?

        public GameState gameState;
        public int numRoles;
        string sceneName;

        public GameServer(Mode mode, CreateHostedClientDelegate clientDelegate = null) : base(mode, clientDelegate)
        {
            Log.Debug("Creating GameServer: {0} {1}", mode, serverOnly ? "dedicated" : "hosted");

            gameState = GameState.NoGame;
            numRoles = 0;
            sceneName = "";
        }

        // only online mode
        public override void WriteRemoteClientData(List<MessageBase> messages)
        {
            base.WriteRemoteClientData(messages);

            // TODO pass role data to clients?

            messages.Add(new GameStatusMessage(gameState, numRoles, sceneName));
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