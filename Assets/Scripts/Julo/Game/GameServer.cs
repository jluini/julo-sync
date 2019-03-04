using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

using Julo.Logging;
using Julo.Network;

namespace Julo.Game
{
    public enum GameState { Unknown, NoGame, Preparing, Playing, GameOver }


    public class GameServer : DualServer
    {
        public new static GameServer instance = null;

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

            int role = GetNextRole();
            bool ready = mode == Mode.OfflineMode;
            string username = "Jorge"; // TODO

            gamePlayer.Init(role, ready, username);
        }

        public override void WritePlayer(IDualPlayer player, List<MessageBase> messageStack)
        {
            base.WritePlayer(player, messageStack);

            var gamePlayer = DNM.GetPlayerAs<GamePlayer>(player);
            messageStack.Add(new GamePlayerMessage(gamePlayer.role, gamePlayer.isReady, gamePlayer.username));
        }

        ////////// Roles //////////

        public void ChangeReady(int connectionId, bool newReady)
        {
            if(gameState != GameState.NoGame)
            {
                Log.Error("Cannot change ready now");
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
                Log.Warn("No role to change?");
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
        
        int GetMaxPlayers()
        {
            return 2; // TODO !!!
        }
        
        ////////// * //////////

        public void TryToStartGame()
        {
            Log.Warn("TryToStartGame not implemented");
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