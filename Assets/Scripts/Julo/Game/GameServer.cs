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

        public const short SpecRole = 0;
        public const short FirstPlayerRole = 1;

        public GameContext gameContext;
        
        Dictionary<int, bool> clientsAreReadyToStart;

        protected Dictionary<int, List<GamePlayer>> playersByRole;

        int willStartCountDown = 1;

        public GameServer(Mode mode, DualPlayer playerModel) : base(mode, playerModel)
        {
            instance = this;

            gameContext = new GameContext();

            playersByRole = new Dictionary<int, List<GamePlayer>>();
        }

        // only online mode
        public override void WriteRemoteClientData(ListOfMessages listOfMessages)
        {
            base.WriteRemoteClientData(listOfMessages);

            listOfMessages.Add(gameContext.GetSnapshot());
        }

        ////////// Player //////////

        // server
        protected override void OnPlayerAdded(DualPlayer player)
        {
            base.OnPlayerAdded(player);

            var gamePlayer = (GamePlayer)player;

            var playerState = GamePlayerState.NoGame;
            int role = gameContext.gameState == GameState.NoGame ? GetNextRole() : GameServer.SpecRole;
            bool ready = mode == Mode.OfflineMode;
            string username = System.String.Format("Player {0}", player.ControllerId()); // TODO

            gamePlayer.Init(playerState, role, ready, username);

            if(!playersByRole.ContainsKey(role))
            {
                playersByRole.Add(role, new List<GamePlayer>());
            }
            playersByRole[role].Add(gamePlayer);
        }

        protected override void OnPlayerRemoved(DualPlayer player)
        {
            var gamePlayer = (GamePlayer)player;
            if(!playersByRole.ContainsKey(gamePlayer.role))
            {
                Log.Error("Role {0} not found", gamePlayer.role);
                return;
            }

            if(!playersByRole[gamePlayer.role].Remove(gamePlayer))
            {
                Log.Error("Could not remove");
            }
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
                //Log.Warn("Not enough players");
                return;
            }

            gameContext.gameState = GameState.WillStart;

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
            if(gameContext.gameState != GameState.WillStart/* && gameContext.gameState != GameState.CancelingStart*/)
            {
                Log.Error("Unexpected call of PrepareToStartGame: {0}", gameContext.gameState);
                gameContext.gameState = GameState.NoGame;
                return;
            }

            gameContext.gameState = GameState.Preparing;

            clientsAreReadyToStart = new Dictionary<int, bool>();

            // TODO track only playing connections?
            foreach(var c in dualContext.AllConnections())
            {
                clientsAreReadyToStart.Add(c.connectionId, false);
            }

            for(int r = 1; r <= GetMaxPlayers(); r++)
            {
                foreach(var p in GetPlayersForRole(r))
                {
                    if(p.playerState != GamePlayerState.NoGame)
                    {
                        Log.Warn("Player {0}:{1} state is {2}", p.ConnectionId(), p.ControllerId(), p.playerState);
                    }

                    p.playerState = GamePlayerState.Playing;
                }
            }

            DualNetworkManager.instance.LoadSceneAsync(gameContext.sceneName, () =>
            {
                var prepareMessage = new PrepareToStartMessage(gameContext.numRoles, gameContext.sceneName);
                var listOfMessages = new ListOfMessages();
                listOfMessages.Add(prepareMessage);

                OnPrepareToStart(listOfMessages);

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

        protected abstract void OnPrepareToStart(ListOfMessages listOfMessages);
        protected abstract void OnStartGame();

        ////////// Roles //////////

        public void ChangeReady(int connectionId, bool newReady)
        {
            if(gameContext.gameState != GameState.NoGame && gameContext.gameState != GameState.WillStart)
            {
                Log.Error("Cannot change ready now");
                // TODO return?
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
                Log.Warn("Cannot change role now ({0})", gameContext.gameState);
                return;
            }

            int currentRole = player.role;

            int newRole;
            if(currentRole == GameServer.SpecRole)
            {
                newRole = GameServer.FirstPlayerRole;
            }
            else
            {
                newRole = currentRole + 1;
                if(newRole > GetMaxPlayers())
                {
                    // no spec role in offline mode
                    newRole = mode == Mode.OfflineMode ? GameServer.FirstPlayerRole : GameServer.SpecRole;
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
            return mode == Mode.OfflineMode ? GameServer.FirstPlayerRole : GameServer.SpecRole;
        }

        protected List<GamePlayer> GetPlayersForRole(int role)
        {
            if(role < 1 || role > GetMaxPlayers())
            {
                Log.Warn("Invalid role: {0}", role);
            }

            if(!playersByRole.ContainsKey(role))
            {
                Log.Warn("Role not found: {0}", role);
                return new List<GamePlayer>();
            }

            return playersByRole[role];
        }

        int NumberOfPlayersForRole(int role)
        {
            if(role < GameServer.FirstPlayerRole || role > GetMaxPlayers())
            {
                Log.Error("Invalid role number: {0}", role);
                return 0;
            }
            
            if(!playersByRole.ContainsKey(role))
            {
                return 0;
            }

            return playersByRole[role].Count;
        }

        ////////// * //////////
        
        int GetMaxPlayers()
        {
            return gameContext.numRoles;
        }

        // //////////////

        bool PlayersAreReady()
        {
            foreach(var playerList in playersByRole.Values)
            {
                foreach(var gamePlayer in playerList)
                {
                    if(!gamePlayer.IsSpectator() && !gamePlayer.isReady)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        bool EnoughPlayersForEachRole()
        {
            for(int role = 1; role <= GetMaxPlayers(); role++)
            {
                if(NumberOfPlayersForRole(role) < 1)
                {
                    Log.Warn("There is no player for role {0}", role);
                    return false;
                }

            }

            return true;
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


        bool ConnectionIsPlaying(ConnectionInfo connection)
        {
            foreach(var p in connection.players.Values)
            {
                var gamePlayer = (GamePlayer)p;
                if(gamePlayer.role != GameServer.SpecRole)
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnClientDisconnected(int connectionId)
        {
            if(gameContext.gameState == GameState.NoGame || gameContext.gameState == GameState.CancelingStart)
            {
                RemoveClient(connectionId);
                return;
            }

            var conn = dualContext.GetConnection(connectionId);

            bool isPlaying = ConnectionIsPlaying(conn);

            if(!isPlaying)
            {
                RemoveClient(connectionId);
                return;
            }

            if(gameContext.gameState == GameState.WillStart)
            {
                // cancel WillStart
                gameContext.gameState = GameState.CancelingStart;

                RemoveClient(connectionId);
                return;
            }

            if(gameContext.gameState == GameState.Preparing || gameContext.gameState == GameState.Playing || gameContext.gameState == GameState.GameOver)
            {

                var nextPlayer = GetNextPlayerToRemove(conn);

                while(nextPlayer != null)
                {
                    if(nextPlayer.role == GameServer.SpecRole)
                    {
                        RemovePlayer(nextPlayer);
                    }


                    nextPlayer = GetNextPlayerToRemove(conn);
                }
            }
            else
            {
                Log.Error("Unexpected case: {0}", gameContext.gameState);
                RemoveClient(connectionId);
            }
        }

        GamePlayer GetNextPlayerToRemove(ConnectionInfo conn)
        {
            foreach(var p in conn.players.Values)
            {
                var gamePlayer = (GamePlayer)p;

                if(gamePlayer.role == GameServer.SpecRole)
                {
                    return gamePlayer;
                }
                switch(gamePlayer.playerState)
                {
                    case GamePlayerState.Disconnected:
                        Log.Debug("Already disconnected");
                        break;

                    case GamePlayerState.Playing:
                    case GamePlayerState.Resigned:
                        DisconnectPlayer(gamePlayer);
                        break;
                    case GamePlayerState.NoGame:
                        Log.Warn("Unexpected case");
                        break;
                }
            }

            return null;
        }

        void DisconnectPlayer(GamePlayer player)
        {
            player.SetState(GamePlayerState.Disconnected);
            // TODO send to remote only
            SendToAll(MsgType.PlayerDisconnected, new DualPlayerSnapshot(player));
            OnPlayerDisconnected(player);
        }

        protected virtual void OnPlayerDisconnected(GamePlayer player)
        {
            // noop
        }


} // class GameServer

} // namespace Julo.Game
