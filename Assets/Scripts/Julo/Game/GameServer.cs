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

        Dictionary<int, bool> clientsAreReadyToStart;

        List<GamePlayer>[] playersPerRole;

        protected int numRoles;
        protected GameState gameState;
        protected string sceneName;

        int willStartCountDown = 1;

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
        }

        ////////// Player //////////

        // server
        public override void OnPlayerAdded(IDualPlayer player)
        {
            base.OnPlayerAdded(player);

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);

            // start as spec if game already started

            int role = gameState == GameState.NoGame ? GetNextRole() : DNM.SpecRole;
            bool ready = mode == Mode.OfflineMode;
            string username = System.String.Format("Player {0}", player.PlayerId()); // TODO

            gamePlayer.Init(role, ready, username);
        }

        public override void WritePlayer(IDualPlayer player, List<MessageBase> messageStack)
        {
            base.WritePlayer(player, messageStack);

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);
            messageStack.Add(new GamePlayerMessage(gamePlayer.role, gamePlayer.isReady, gamePlayer.username));
        }

        ////////// * //////////

        public void TryToStartGame()
        {
            if(gameState != GameState.NoGame)
            {
                Log.Error("Unexpected call of TryToStartGame: {0}", gameState);
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

            gameState = GameState.WillStart;

            numRoles = 2; // TODO 
            sceneName = "beach"; // TODO 

            CollectPlayersPerRole();

            DualNetworkManager.instance.StartCoroutine(WillStartCoroutine());
        }

        IEnumerator WillStartCoroutine()
        {
            for(int remainingSeconds = willStartCountDown; remainingSeconds > 0; remainingSeconds--)
            {
                SendToAll(MsgType.GameWillStart, new IntegerMessage(remainingSeconds));

                yield return new WaitForSeconds(1f);

                if(gameState != GameState.WillStart)
                {
                    if(gameState != GameState.CancelingStart)
                    {
                        Log.Warn("WillStartCoroutine: unexpected state {0}", gameState);
                    }

                    SendToAll(MsgType.GameCanceled, new EmptyMessage());

                    gameState = GameState.NoGame;

                    yield break;
                }
                    
            }

            PrepareToStartGame();

            yield break;
        }

        void PrepareToStartGame()
        {
            if(gameState != GameState.WillStart && gameState != GameState.CancelingStart)
            {
                Log.Error("Unexpected call of PrepareToStartGame: {0}", gameState);
                return;
            }

            gameState = GameState.Preparing;

            DualNetworkManager.instance.LoadSceneAsync(sceneName, () =>
            {
                var messageStack = new List<MessageBase>();
                messageStack.Add(new PrepareToStartMessage(numRoles, sceneName));

                OnPrepareToStart(messageStack);

                SendToAll(MsgType.PrepareToStart, new MessageStackMessage(messageStack));

                // waiting to all clients sending ReadyToStart message
            });
        }

        public void StartGame()
        {
            gameState = GameState.Playing;

            OnStartGame();

            SendToAll(MsgType.StartGame, new EmptyMessage()); //  MessageStackMessage(messageStack)); // TODO send initial game data
        }

        protected abstract void OnPrepareToStart(List<MessageBase> messageStack);
        protected abstract void OnStartGame();

        ////////// * //////////

        ////////// Roles //////////

        public void ChangeReady(int connectionId, bool newReady)
        {
            if(gameState != GameState.NoGame && gameState != GameState.WillStart)
            {
                Log.Error("Cannot change ready now");
            }

            if(gameState == GameState.WillStart)
            {
                // cancel WillStart
                gameState = GameState.CancelingStart;
            }

            var players = connections.GetConnection(connectionId).players;

            foreach(var playerData in players)
            {
                var gamePlayer = connections.GetPlayerAs<GamePlayer>(playerData.playerData.playerId);
                //gamePlayer.SetReady(newReady);
            }

            SendToAll(MsgType.ChangeReady, new ChangeReadyMessage(connectionId, newReady));
        }

        public void ChangeRole(GamePlayer player)
        {
            if(gameState != GameState.NoGame)
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

                SendToAll(MsgType.ChangeRole, new ChangeRoleMessage(player.PlayerId(), newRole));
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

            foreach(var c in connections.AllConnections().Values)
            {
                foreach(var player in c.players)
                {
                    var playerId = player.playerData.playerId;
                    var gamePlayer = connections.GetPlayerAs<GamePlayer>(playerId);

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
            playersPerRole = new List<GamePlayer>[numRoles];

            clientsAreReadyToStart = new Dictionary<int, bool>();

            foreach(var c in connections.AllConnections().Values)
            {
                clientsAreReadyToStart[c.connectionId] = false;

                foreach(var p in c.players)
                {
                    var gamePlayer = connections.GetPlayerAs<GamePlayer>(p);

                    if(!gamePlayer.IsSpectator())
                    {

                        int role = gamePlayer.role;

                        if(role < 1 || role > numRoles)
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

        protected List<GamePlayer> GetPlayersForRole(int role)
        {
            return playersPerRole[role - 1];
        }

        int GetMaxPlayers()
        {
            return numRoles;
        }

        // //////////////

        bool PlayersAreReady()
        {
            foreach(var p in connections.AllPlayers<GamePlayer>())
            {
                if(!p.IsSpectator() && !p.isReady)
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
                case MsgType.ChangeReady:

                    var changeReadyMsg = message.ReadInternalMessage<ChangeReadyMessage>();
                    var newReady = changeReadyMsg.newReady;

                    ChangeReady(from, newReady);

                    break;

                case MsgType.ReadyToStart:
                    if(gameState != GameState.Preparing)
                    {
                        Log.Warn("Unexpected ReadyToStart in state {0}", gameState);
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

        ///

        void SetState(GameState newState)
        {
            // TODO mostrar cambio?
            this.gameState = newState;
        }

    } // class GameServer

} // namespace Julo.Game