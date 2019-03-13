using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public abstract class GameClient : DualClient
    {
        public new static GameClient instance = null;

        // only hosted
        protected GameServer gameServer;

        // only remote
        GameContext clientContext;

        protected GameContext gameContext
        {
            get
            {
                if(isHosted)
                {
                    return gameServer.gameContext;
                }
                else
                {
                    return clientContext;
                }
            }
        }

        public GameClient(Mode mode, DualServer dualServer, DualPlayer playerModel) : base(mode, dualServer, playerModel)
        {
            instance = this;
            if(dualServer != null)
            {
                gameServer = (GameServer)dualServer;
            }
        }

        public sealed override void InitializeState(int connectionNumber, ListOfMessages listOfMessages)
        {
            base.InitializeState(connectionNumber, listOfMessages);
            
            var message = listOfMessages.ReadMessage<GameContextSnapshot>();

            clientContext = new GameContext(message);

            switch(clientContext.gameState)
            {
                case GameState.NoGame:
                case GameState.WillStart:
                    // I joined but no game yet
                    OnLobbyJoin(listOfMessages);
                    break;

                case GameState.Preparing:
                case GameState.Playing:
                case GameState.GameOver:
                    DualNetworkManager.instance.LoadSceneAsync(clientContext.sceneName, () =>
                    {
                        OnLateJoin(listOfMessages);
                    });

                    break;
            }
        }
        
        // only remote
        protected override void OnPlayerAdded(DualPlayer player, ListOfMessages listOfMessages)
        {
            base.OnPlayerAdded(player, listOfMessages);

            var gamePlayerSnapshot = listOfMessages.ReadMessage<GamePlayerSnapshot>();
            
            // TODO cast or cache GamePlayer?
            var gamePlayer = (GamePlayer)player;

            gamePlayer.Init(gamePlayerSnapshot);
        }
        
        // //////////////////////

        public void OnReadyChanged(bool newReady)
        {
            SendToServer(MsgType.ChangeReady, new ChangeReadyMessage(dualContext.localConnectionNumber, newReady));
        }
        
        // //////////////////////
        
        void OnPrepareToStartMessage(ListOfMessages listOfMessages)
        {
            var prepareMessage = listOfMessages.ReadMessage<PrepareToStartMessage>();

            // TODO this is already synchronized?
            gameContext.numRoles = prepareMessage.numRoles;
            gameContext.sceneName = prepareMessage.sceneName;

            if(isHosted)
            {
                SendToServer(MsgType.ReadyToStart, new EmptyMessage());
            }
            else
            {
                DualNetworkManager.instance.LoadSceneAsync(gameContext.sceneName, () =>
                {
                    OnPrepareToStart(listOfMessages);
                    SendToServer(MsgType.ReadyToStart, new EmptyMessage());
                });
            }
        }

        // //////////////////////

        protected abstract void OnPrepareToStart(ListOfMessages listOfMessages);

        protected virtual void OnGameStarted()
        {
            // TODO
        }


        protected abstract void OnLobbyJoin(ListOfMessages listOfMessages);
        protected abstract void OnLateJoin(ListOfMessages listOfMessages);

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

                        var players = dualContext.GetPlayers(connectionId);

                        foreach(var player in players)
                        {
                            // TODO cast or cache game players?
                            var gamePlayer = (GamePlayer)player;
                            gamePlayer.SetReady(newReady);
                        }
                    }

                    break;

                case MsgType.ChangeRole:

                    if(!isHosted)
                    {
                        var changeRoleMsg = message.ReadInternalMessage<ChangeRoleMessage>();
                        var connId = changeRoleMsg.connectionId;
                        var controllerId = changeRoleMsg.controllerId;
                        var newRole = changeRoleMsg.newRole;

                        // TODO cast or cache?
                        var gamePlayer = (GamePlayer)dualContext.GetPlayer(connId, controllerId);
                        gamePlayer.SetRole(newRole);
                    }

                    break;

                case MsgType.ChangeUsername:

                    if(!isHosted)
                    {
                        var usernameMsg = message.ReadInternalMessage<ChangeUsernameMessage>();
                        var connId = usernameMsg.connectionId;
                        var controllerId = usernameMsg.controllerId;
                        var newName = usernameMsg.newName;

                        // TODO cast or cache?
                        var gamePlayer = (GamePlayer)dualContext.GetPlayer(connId, controllerId);
                        gamePlayer.SetUsername(newName);
                    }

                    break;

                case MsgType.GameWillStart:

                    var remainingSecsMessage = message.ReadInternalMessage<IntegerMessage>();
                    var secs = remainingSecsMessage.value;

                    Log.Debug("Game will start in {0} secs...", secs);

                    gameContext.gameState = GameState.WillStart;

                    break;

                case MsgType.GameCanceled:
                    gameContext.gameState = GameState.NoGame;
                    Log.Debug("Game canceled");
                    break;

                case MsgType.PrepareToStart:
                    gameContext.gameState = GameState.Preparing;

                    var listOfMessages = message.ReadInternalMessage<ListOfMessages>();

                    OnPrepareToStartMessage(listOfMessages);

                    break;

                case MsgType.StartGame:
                    Log.Debug("Game started!");

                    OnGameStarted();
                    break;

                case MsgType.PlayerDisconnected:
                    if(!isHosted)
                    {
                        var playerDisconnectedMsg = message.ReadInternalMessage<DualPlayerSnapshot>();

                        var connId = playerDisconnectedMsg.connectionId;
                        var controllerId = playerDisconnectedMsg.controllerId;

                        // TODO cast or cache?
                        var gamePlayer = (GamePlayer)dualContext.GetPlayer(connId, controllerId);

                        gamePlayer.SetState(GamePlayerState.Disconnected);
                    }

                    break;

                default:
                    base.OnMessage(message);
                    break;
            }
        }

        // Checking

        void CheckState(GameState expectedState)
        {
            if(gameContext.gameState != expectedState)
            {
                WrongState(gameContext.gameState.ToString());
            }
        }

        void CheckState(GameState[] expectedStates)
        {
            if(!System.Array.Exists<GameState>(expectedStates, s => s == gameContext.gameState))
            {
                WrongState(gameContext.gameState.ToString());
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

            SendToServer(MsgType.ChangeUsername, new ChangeUsernameMessage(gamePlayer.ConnectionId(), gamePlayer.ControllerId(), newName));

            return true;
        }
        
    } // class GameClient

} // namespace Julo.Game
