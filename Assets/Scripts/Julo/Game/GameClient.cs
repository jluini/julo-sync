using System.Collections.Generic;

using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public abstract class GameClient : DualClient
    {
        public new static GameClient instance = null;

        protected GameState gameState;
        protected int numRoles;
        protected string sceneName;

        // only remote client
        Dictionary<uint, GamePlayerMessage> pendingPlayers = new Dictionary<uint, GamePlayerMessage>();

        public GameClient(Mode mode, DualServer server = null) : base(mode, server)
        {
            instance = this;

            this.gameState = GameState.Unknown;
            this.numRoles = 0;
            this.sceneName = "";
        }

        public sealed override void InitializeState(int connectionNumber, MessageStackMessage messageStack)
        {
            base.InitializeState(connectionNumber, messageStack);

            var message = messageStack.ReadMessage<GameStatusMessage>();

            gameState = message.state;
            numRoles = message.numRoles;
            sceneName = message.sceneName;

            switch(gameState)
            {
                case GameState.NoGame:
                case GameState.WillStart:
                    Log.Debug("I joined but no game yet");
                    break;

                case GameState.Preparing:
                case GameState.Playing:
                case GameState.GameOver:
                    DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
                    {
                        OnLateJoin(messageStack);
                    });

                    break;
            }
        }

        public override void ReadPlayer(DualPlayerMessage dualPlayerData, MessageStackMessage messageStack)
        {
            base.ReadPlayer(dualPlayerData, messageStack);

            var gamePlayerData = messageStack.ReadMessage<GamePlayerMessage>();

            pendingPlayers.Add(dualPlayerData.playerId, gamePlayerData);
        }

        protected override void OnPlayerResolved(OnlineDualPlayer player, DualPlayerMessage playerScreenshot)
        {
            base.OnPlayerResolved(player, playerScreenshot);

            var netId = playerScreenshot.PlayerId();

            if(player.PlayerId() != netId)
            {
                Log.Error("Not resolved! {0} != {1}", player.PlayerId(), netId);
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
            bool ready = gamePlayerMessage.isReady;

            string username = gamePlayerMessage.username;

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);

            if(gamePlayer.role == role)
                Log.Warn("Role already set to {0}", role);

            if(gamePlayer.username == username)
                Log.Warn("Username already set to {0}", username);

            gamePlayer.Init(gameState, role, ready, username);

            // Log.Debug("Resolved {0} to {1}:{2}", player.PlayerId(), role, username);
        }

        // //////////////////////
        
        public void OnReadyChanged(bool newReady)
        {
            SendToServer(MsgType.ChangeReady, new ChangeReadyMessage(connections.localConnectionNumber, newReady));
        }
        
        // //////////////////////
        
        void OnPrepareToStartMessage(MessageStackMessage messageStack)
        {
            var prepareMessage = messageStack.ReadMessage<PrepareToStartMessage>();

            numRoles = prepareMessage.numRoles;
            sceneName = prepareMessage.sceneName;

            if(isHosted)
            {
                SendToServer(MsgType.ReadyToStart, new EmptyMessage());
                return;
            }

            DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
            {
                OnPrepareToStart(messageStack);
                SendToServer(MsgType.ReadyToStart, new EmptyMessage());
            });
        }

        // //////////////////////

        protected abstract void OnPrepareToStart(MessageStackMessage messageStack);


        protected virtual void OnGameStarted()
        {
            // TODO cache GamePlayers!!!
            foreach(var playerInfo in connections.AllPlayers())
            {
                var gamePlayer = connections.GetPlayerAs<GamePlayer>(playerInfo);
                gamePlayer.GameStarted();
            }
        }


        protected abstract void OnLateJoin(MessageStackMessage messageStack);

        protected override void OnMessage(WrappedMessage message)
        {
            switch(message.messageType)
            {
                case MsgType.ChangeReady:
                    if(!isHosted)
                    {
                        var changeReadyMessage = message.ReadInternalMessage<ChangeReadyMessage>();

                        var connectionId = changeReadyMessage.connectionId;
                        var newReady = changeReadyMessage.newReady;

                        var players = connections.GetConnection(connectionId).players;

                        foreach(var playerInfo in players)
                        {
                            // TODO cache game players!!!
                            var gamePlayer = connections.GetPlayerAs<GamePlayer>(playerInfo);
                            gamePlayer.SetReady(newReady);
                        }
                    }

                    break;

                case MsgType.ChangeRole:

                    if(!isHosted)
                    {
                        var changeRoleMsg = message.ReadInternalMessage<ChangeRoleMessage>();
                        var playerId = changeRoleMsg.playerId;
                        var newRole = changeRoleMsg.newRole;

                        connections.GetPlayerAs<GamePlayer>(playerId).SetRole(newRole);
                    }

                    break;

                case MsgType.ChangeUsername:

                    if(!isHosted)
                    {
                        var usernameMsg = message.ReadInternalMessage<ChangeUsernameMessage>();
                        var playerId = usernameMsg.playerId;
                        var newName = usernameMsg.newName;

                        connections.GetPlayerAs<GamePlayer>(playerId).SetUsername(newName);
                    }

                    break;

                case MsgType.GameWillStart:

                    var remainingSecsMessage = message.ReadInternalMessage<IntegerMessage>();
                    var secs = remainingSecsMessage.value;

                    Log.Debug("Game will start in {0} secs...", secs);

                    gameState = GameState.WillStart;

                    break;

                case MsgType.GameCanceled:
                    gameState = GameState.NoGame;
                    Log.Debug("Game canceled");
                    break;

                case MsgType.PrepareToStart:
                    gameState = GameState.Preparing;

                    var messageStack = message.ReadInternalMessage<MessageStackMessage>();

                    OnPrepareToStartMessage(messageStack);

                    break;

                case MsgType.StartGame:
                    Log.Debug("Game started!");

                    OnGameStarted();
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
        
        public bool ChangeName(GamePlayer gamePlayer, string newName)
        {
            if(!gamePlayer.IsLocal())
            {
                Log.Error("Unexpected call of OnNameEntered");
                return false;
            }

            // TODO validate name

            newName = newName.Trim();

            if(newName.Length == 0)
            {
                Log.Error("Invalid name");
                return false;
            }

            SendToServer(MsgType.ChangeUsername, new ChangeUsernameMessage(gamePlayer.PlayerId(), newName));

            return true;
        }
        
        /// 

        void SetState(GameState newState)
        {
            // TODO mostrar cambio?
            this.gameState = newState;
        }


    } // class GameClient

} // namespace Julo.Game