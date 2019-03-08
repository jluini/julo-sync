using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public enum GameState { Unknown, NoGame, WillStart, CancelingStart, Preparing, Playing, GameOver }

    public abstract class GameServer : DualServer
    {
        public new static GameServer instance = null;

        public GameContext gameContext;
        
        Dictionary<int, bool> clientsAreReadyToStart;

        List<GamePlayer>[] playersPerRole;

        int willStartCountDown = 1;

        public GameServer(Mode mode, DualPlayer playerModel) : base(mode, playerModel)
        {
            instance = this;

            gameContext = new GameContext();
        }

        // only online mode
        public override void WriteRemoteClientData(ListOfMessages listOfMessages)
        {
            base.WriteRemoteClientData(listOfMessages);

            listOfMessages.Add(gameContext.GetSnapshot());
        }

        ////////// Player //////////

        // server
        public override void OnPlayerAdded(DualPlayer player)
        {
            base.OnPlayerAdded(player);

            // TODO cast or cache GamePlayer?
            var gamePlayer = (GamePlayer)player;

            // start as spec if game already started

            GamePlayerState playerState;

            if(gameContext.gameState == GameState.NoGame)
            {
                playerState = GamePlayerState.NoGame;
            }
            else
            {
                Log.Error("Adding players in playing mode not supported");
                playerState = GamePlayerState.NoGame;
            }

            int role = gameContext.gameState == GameState.NoGame ? GetNextRole() : DNM.SpecRole;
            bool ready = mode == Mode.OfflineMode;
            string username = System.String.Format("Player {0}", player.ControllerId()); // TODO

            gamePlayer.Init(playerState, role, ready, username);
        }

        public override void WritePlayer(DualPlayer player, ListOfMessages listOfMessages)
        {
            base.WritePlayer(player, listOfMessages);

            var gamePlayer = (GamePlayer)player;
            listOfMessages.Add(new GamePlayerSnapshot(gamePlayer));
        }

        ////////// * //////////

        public void TryToStartGame()
        {
            if(gameContext.gameState != GameState.NoGame)
            {
                Log.Error("Unexpected call of TryToStartGame: {0}", gameContext.gameState);
                return;
            }
            if(!PlayersAreReady())
            {
                Log.Warn("All players must be ready");
                return;
            }

            if(!EnoughPlayersForEachRole())
            {
                Log.Warn("Not enough players");
                return;
            }

            gameContext.gameState = GameState.WillStart;

            CollectPlayersPerRole();

            DualNetworkManager.instance.StartCoroutine(WillStartCoroutine());
        }

        IEnumerator WillStartCoroutine()
        {
            for(int remainingSeconds = willStartCountDown; remainingSeconds > 0; remainingSeconds--)
            {
                SendToAll(MsgType.GameWillStart, new IntegerMessage(remainingSeconds));

                yield return new WaitForSeconds(1f);

                if(gameContext.gameState != GameState.WillStart)
                {
                    if(gameContext.gameState != GameState.CancelingStart)
                    {
                        Log.Warn("WillStartCoroutine: unexpected state {0}", gameContext.gameState);
                    }

                    SendToAll(MsgType.GameCanceled, new EmptyMessage());

                    gameContext.gameState = GameState.NoGame;

                    yield break;
                }
                    
            }

            PrepareToStartGame();

            yield break;
        }

        void PrepareToStartGame()
        {
            if(gameContext.gameState != GameState.WillStart && gameContext.gameState != GameState.CancelingStart)
            {
                Log.Error("Unexpected call of PrepareToStartGame: {0}", gameContext.gameState);
                return;
            }

            gameContext.gameState = GameState.Preparing;

            DualNetworkManager.instance.LoadSceneAsync(gameContext.sceneName, () =>
            {
                var prepareMessage = new PrepareToStartMessage(gameContext.numRoles, gameContext.sceneName);
                var listOfMessages = new ListOfMessages();
                listOfMessages.Add(prepareMessage);

                OnPrepareToStart(playersPerRole, listOfMessages);

                SendToAll(MsgType.PrepareToStart, listOfMessages);

                // waiting to all clients sending ReadyToStart message
            });
        }

        public void StartGame()
        {
            gameContext.gameState = GameState.Playing;

            OnStartGame();

            SendToAll(MsgType.StartGame, new EmptyMessage());
        }

        protected abstract void OnPrepareToStart(List<GamePlayer>[] playersPerRole, ListOfMessages listOfMessages);
        protected abstract void OnStartGame();

        ////////// Roles //////////

        public void ChangeReady(int connectionId, bool newReady)
        {
            if(gameContext.gameState != GameState.NoGame && gameContext.gameState != GameState.WillStart)
            {
                Log.Error("Cannot change ready now");
            }

            if(gameContext.gameState == GameState.WillStart)
            {
                // cancel WillStart
                gameContext.gameState = GameState.CancelingStart;
            }

            var players = dualContext.GetPlayers(connectionId);

            foreach(var player in players)
            {
                var gamePlayer = (GamePlayer)player;
                gamePlayer.SetReady(newReady);
            }

            // TODO send to remote
            SendToAll(MsgType.ChangeReady, new ChangeReadyMessage(connectionId, newReady));
        }

        public void ChangeRole(GamePlayer player)
        {
            if(gameContext.gameState != GameState.NoGame)
            {
                Log.Error("Unexpected call of ChangeRole");
                return;
            }

            int currentRole = player.role;

            int newRole;
            if(currentRole == DNM.SpecRole)
            {
                newRole = DNM.FirstPlayerRole;
            }
            else
            {
                newRole = currentRole + 1;
                if(newRole > GetMaxPlayers())
                {
                    // no spec role in offline mode
                    newRole = mode == Mode.OfflineMode ? DNM.FirstPlayerRole : DNM.SpecRole;
                }
            }

            if(newRole != currentRole)
            {
                // sets role in server
                player.SetRole(newRole);

                SendToAll(MsgType.ChangeRole, new ChangeRoleMessage(player.ConnectionId(), player.ControllerId(), newRole));
            }
            else
            {
                Log.Error("No role to change?");
            }
        }

        int GetNextRole()
        {
            for(int i = 1; i <= GetMaxPlayers(); i++)
            {
                var playerCount = NumberOfPlayersForRole(i);
                if(playerCount == 0)
                {
                    return i;
                }
            }

            // no spec role in offline mode
            return mode == Mode.OfflineMode ? DNM.FirstPlayerRole : DNM.SpecRole;
        }


        int NumberOfPlayersForRole(int role)
        {
            if(role < DNM.FirstPlayerRole || role > GetMaxPlayers())
            {
                Log.Error("Invalid role number: {0}", role);
                return 0;
            }

            int ret = 0;

            foreach(var c in dualContext.AllConnections())
            {
                var players = c.GetPlayers();
                foreach(var player in players)
                {
                    // TODO cache GamePlayers !!!
                    var gamePlayer = (GamePlayer)player;

                    if(gamePlayer.role == role)
                    {
                        ret++;
                    }
                }
            }

            return ret;
        }

        ////////// * //////////

        void CollectPlayersPerRole()
        {
            playersPerRole = new List<GamePlayer>[gameContext.numRoles];

            clientsAreReadyToStart = new Dictionary<int, bool>();

            foreach(var c in dualContext.AllConnections())
            {
                clientsAreReadyToStart[c.connectionId] = false;

                foreach(var player in c.GetPlayers())
                {
                    var gamePlayer = (GamePlayer)player;
                    if(!gamePlayer.IsSpectator())
                    {
                        int role = gamePlayer.role;

                        if(role < 1 || role > gameContext.numRoles)
                        {
                            Log.Error("Unexpected role {0}", role);
                            return;
                        }

                        if(playersPerRole[role - 1] == null)
                        {
                            playersPerRole[role - 1] = new List<GamePlayer>();
                        }

                        playersPerRole[role - 1].Add(gamePlayer);
                    }
                }
            }
        }
        
        int GetMaxPlayers()
        {
            return gameContext.numRoles;
        }

        // //////////////

        bool PlayersAreReady()
        {
            // TODO cache GamePlayers !!!
            foreach(var dualPlayer in dualContext.AllPlayers())
            {
                // TODO cast or cache?
                var gamePlayer = (GamePlayer)dualPlayer;

                if(!gamePlayer.IsSpectator() && !gamePlayer.isReady)
                {
                    return false;
                }
            }

            return true;
        }

        bool EnoughPlayersForEachRole()
        {
            bool enoughPlayers = true;

            for(int role = 1; role <= GetMaxPlayers(); role++)
            {
                if(NumberOfPlayersForRole(role) < 1)
                {
                    enoughPlayers = false;
                    Log.Debug("Role {0} not satisfied", role);
                }
            }

            return enoughPlayers;
        }

        bool AllClientsAreReadyToStart()
        {
            foreach(var t in clientsAreReadyToStart.Values)
            {
                if(!t)
                    return false;
            }
            return true;
        }

        ////////// Messaging //////////

        protected override void OnMessage(WrappedMessage message, int from)
        {
            switch(message.messageType)
            {
                case MsgType.ChangeUsername:

                    var usernameMsg = message.ReadInternalMessage<ChangeUsernameMessage>();

                    // TODO validate name

                    var connId = usernameMsg.connectionId;
                    var controllerId = usernameMsg.controllerId;
                    var newName = usernameMsg.newName;

                    var player = dualContext.GetPlayer(connId, controllerId);
                    var gamePlayer = (GamePlayer)player;

                    if(gamePlayer == null)
                    {
                        Log.Error("Player to change name not found");
                        return;
                    }

                    gamePlayer.SetUsername(newName);

                    SendToAll(MsgType.ChangeUsername, usernameMsg);

                    break;

                case MsgType.ChangeReady:

                    var changeReadyMsg = message.ReadInternalMessage<ChangeReadyMessage>();
                    var newReady = changeReadyMsg.newReady;

                    if(changeReadyMsg.connectionId != from)
                    {
                        Log.Warn("Unmatching connection here {0}!={1}", changeReadyMsg.connectionId, from);
                    }

                    ChangeReady(from, newReady);

                    break;

                case MsgType.ReadyToStart:
                    if(gameContext.gameState != GameState.Preparing)
                    {
                        Log.Warn("Unexpected ReadyToStart in state {0}", gameContext.gameState);
                        return;
                    }
                    if(!clientsAreReadyToStart.ContainsKey(from))
                    {
                        Log.Warn("Unexpected ReadyToStart from {0}", from);
                        return;
                    }

                    if(clientsAreReadyToStart[from])
                    {
                        Log.Warn("Client {0} was already ready");
                        return;
                    }

                    clientsAreReadyToStart[from] = true;

                    if(AllClientsAreReadyToStart())
                    {
                        StartGame();
                    }

                    break;

                default:
                    base.OnMessage(message, from);
                    break;
            }
        }

    } // class GameServer

} // namespace Julo.Game
