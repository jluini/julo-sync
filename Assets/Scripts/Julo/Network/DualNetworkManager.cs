using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

using Julo.Logging;
using Julo.Users;

namespace Julo.Network
{

    // TODO rename to mode?
    public enum DNMState
    {
        NotInitialized,
        Off,
        Offline,

        CreatingHost,
        Host,

        StartingAsClient,
        Client,
        //ClientPlaying
            
        // TODO add dedicated server mode
    }

    public class DualNetworkManager : NetworkManager, DNMListener
    {
        static DualNetworkManager _instance = null;
        public static DualNetworkManager instance
        {
            get
            {
                return _instance;
            }
        }

        public LevelData levelData;

        public enum GameState
        {

            NoGame, // only when not initialized
            //Lobby,  // lobby as server or client

            WillStart, // still unused

            // server: sent initial state to playing clients (Prepare)
            // client: if playing, received Prepare, and sent ReadyToSpawn; then is receiving ObjectSpawn's but not ok too
            //         if late joining...  TODO
            Preparing,

            // server: all playing clients sent SpawnOk so we sent them StartGame
            // client: received StartGame message
            Playing,

            GameOver
        }

        [Header("Players")]

        public OfflinePlayer offlinePlayerModel;
        public OnlinePlayer onlinePlayerModel;

        [Header("Hooks")]
        public Transform playerContainer;
        public Toggle sceneToggle; // TODO remove 


        [Header("Configuration")]
        public GameServer gameServerPrefab;
        public GameClient gameClientPrefab;

        ////////////////////////////////////////////////////////////

        const int LocalConnectionId = 0;

        ////////////////////////////////////////////////////////////

        UserManager userManager = null;
        DNMState state = DNMState.NotInitialized;

        GameState gameState = GameState.NoGame;
        GameServer gameServer = null;
        GameClient gameClient = null;

        NetworkClient localClient = null;

        int numRoles;

        ////////////////////////////////////////////////////////////

        // only server
        Dictionary<int, Client> clients;
        List<Player>[] playersPerRole = null;

        ////////////////////////////////////////////////////////////

        // only offline mode

        uint lastOfflineIdUsed = 0;

        ////////////////////////////////////////////////////////////

        List<DNMListener> listeners = new List<DNMListener>();

        ////////////////////////////////////////////////////////////

        public void Init(UserManager userManager)
        {
            if(_instance != null)
            {
                Log.Error("Duplicated DNM");
                return;
            }
            _instance = this;

            if(state != DNMState.NotInitialized)
            {
                Log.Error("DNM already initialized");
                return;
            }

            SetState(DNMState.Off);
            SetGameState(GameState.NoGame);
            this.userManager = userManager;
        }

        public void StartOffline()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM should be Off");
                return;
            }

            clients = new Dictionary<int, Client>();

            SetState(DNMState.Offline);

            var singleClient = new Client(null);
            clients.Add(LocalConnectionId, singleClient);

            AddOfflinePlayer(userManager.GetActiveUser());

            // TODO delete this!!!
            if(sceneToggle.isOn)
                AddOfflinePlayer(userManager.GetActiveUser());
        }

        public void StartAsHost()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsHost");
                return;
            }

            clients = new Dictionary<int, Client>();

            SetState(DNMState.CreatingHost);

            localClient = StartHost();

            if(localClient == null)
            {
                SetState(DNMState.Off);
                Log.Error("DNM: could not create host");
            }
        }

        public void StartAsClient()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsClient");
                return;
            }

            SetState(DNMState.StartingAsClient);

            localClient = StartClient();

            if(localClient == null)
            {
                Log.Error("DNM: could not start client");
            }
        }

        public void Stop()
        {
            if(state == DNMState.Offline)
            {
                SetState(DNMState.Off);
                foreach(var p in GetClient(LocalConnectionId).players)
                {
                    OfflinePlayer op = (OfflinePlayer)p;
                    Destroy(op.gameObject);
                }
                clients = null;
            }
            else if(state == DNMState.Host)
            {
                // TODO destroy and clear things
                // TODO should clear clients ???????????????
                StopHost();
                clients = null;
            }
            else if(state == DNMState.Client)
            {
                // TODO destroy and clear things
                StopClient();
            }
            else
            {
                Log.Warn("DNM: unexpected call of Stop");
            }
        }

        ////////////////////////////////////////////////////////////

        //////////////////// Offline mode //////////////////////////

        void AddOfflinePlayer(UserProfile user)
        {
            var player = GameObject.Instantiate(offlinePlayerModel, playerContainer) as OfflinePlayer;
            int newRole;

            if(gameState == GameState.NoGame)
            {
                newRole = GetNextRole();
            }
            else if(gameState == GameState.WillStart || gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                newRole = DNM.SpecRole;
            }
            else
            {
                Log.Warn("Unexpected gameState {0}", gameState);
                newRole = DNM.SpecRole;
            }

            player.Init(user, ++lastOfflineIdUsed, newRole);

            GetClient(LocalConnectionId).AddPlayer(player);
        }

        // TODO !!!
        public List<DNMPlayer> OfflinePlayers()
        {
            return GetClient(LocalConnectionId).players;
        }

        ////////////////////////////////////////////////////////////

        /////// SERVER

        // Playing

        public void TryToStartGame()
        {
            if(state != DNMState.Host && state != DNMState.Offline)
            {
                Log.Error("Invalid call of StartGame: {0}", state);
                return;
            }
            if(gameState != GameState.NoGame)
            {
                Log.Error("Invalid call of StartGame: {0}", gameState);
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

            PrepareToStartGame();
        }

        void PrepareToStartGame()
        {
            if(playersPerRole != null)
            {
                Log.Warn("playersPerRole already initialized");
            }

            var sceneName = GetSceneName();

            SetGameState(GameState.Preparing);
            numRoles = levelData.MaxPlayers;
            CollectPlayers();

            LoadSceneAsync(sceneName, () =>
            {
                InstantiateServer();

                // this is just to exclude DedicatedServer mode that doesn't exist yet
                if(state == DNMState.Offline)
                {
                    // instantiate local game client but no messaging
                    InstantiateClient(gameServer, Mode.OfflineMode, numRoles);
                    StartGame();
                    // TODO rest of things!
                }
                else if(state == DNMState.Host)
                {
                    // instantiate local client
                    InstantiateClient(gameServer, Mode.OnlineMode, numRoles);

                    var initialMessages = new List<MessageBase>();
                    gameServer.WriteInitialData(initialMessages);

                    var msg = new StartGameMessage(sceneName, initialMessages);

                    // message to create and initialize remote game clients
                    SendToAll(MsgType.StartGame, msg); // TODO ToAllRemote?
                }
                else
                {
                    Log.Error("DNM: unexpected state call of PrepareToStartGame '{0}'", state);
                }
            });
        }

        void StartGame()
        {
            OnClientGameStarted(); // this is to hide game panel
            SetGameState(GameState.Playing);
            //StartCoroutine(StartGameDelayed());
            gameServer.StartGame();
        }

        public void ChangeRole(DNMPlayer player)
        {
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
        }

        // Server callbacks

        public override void OnServerConnect(NetworkConnection conn)
        {
            // Log.Debug("### OnServerConnect({0}, {1})", state.ToString(), conn.connectionId);

            int id = conn.connectionId;

            if(clients.ContainsKey(id))
            {
                Log.Error("Client already registered");
                return;
            }

            bool accepted = false;

            if(state == DNMState.CreatingHost)
            {
                // this is just the local connection when starting as host
                if(id == 0)
                {
                    accepted = true;
                }
                else
                {
                    Log.Warn("Unexpected connectionId when creating host: {0}", id);
                }
            }
            else if(state == DNMState.Host)
            {
                if(id == 0)
                {
                    Log.Error("Unexpected connectionId=0 when hosting");
                }
                else
                {
                    accepted = !sceneToggle.isOn; // TODO accept criteria
                    SendStatusToClient(accepted, conn);
                }
            }
            else
            {
                Log.Error("OnServerConnect: invalid state {0}", state);
                return;
            }

            if(accepted)
            {
                var client = new Client(conn);
                clients.Add(id, client);
            }
            else
            {
                conn.Disconnect();
            }
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Log.Debug("### OnServerDisconnect({0})", conn.connectionId);

            NetworkServer.DestroyPlayersForConnection(conn);

            var id = conn.connectionId;

            if(clients != null && clients.ContainsKey(id))
            {
                clients.Remove(id);
            }
            else
            {
                Log.Warn("Could not delete from dict");
            }

            if(conn.lastError != NetworkError.Ok)
            {
                Log.Error("ServerDisconnected due to error: " + conn.lastError);
                // if (LogFilter.logError) { Debug.LogError("ServerDisconnected due to error: " + conn.lastError); }
            }

            // Log.Debug("A client disconnected from the server: " + conn);
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            NetworkServer.SetClientReady(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader messageReader)
        {
            if(state != DNMState.Host)
            {
                Log.Warn("Unexpected call of OnServerAddPlayer");
                return;
            }

            CustomAddPlayerMessage msg = messageReader.ReadMessage<CustomAddPlayerMessage>();

            string username = msg.username;

            var player = GameObject.Instantiate(onlinePlayerModel/*, playerContainer not needed */) as OnlinePlayer;

            UserProfile user = new UserProfile(username);
            int role = DNM.SpecRole;

            if(gameState == GameState.NoGame)
            {
                int nextRole = GetNextRole();
                role = nextRole;
            }
            else if(gameState == GameState.WillStart || gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                role = DNM.SpecRole;
            }
            else
            {
                Log.Warn("Unexpected gameState {0}", gameState);
            }

            player.SetUser(user);
            player.SetRole(role);

            GetClient(conn).AddPlayer(player);

            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
            Log.Error("OnServerAddPlayer should not be called without extra message");
        }

        public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player) {
            if(player.gameObject != null)
                NetworkServer.Destroy(player.gameObject);
        }

        public override void OnServerError(NetworkConnection conn, int errorCode) {
            Log.Debug("Server network error occurred: " + (NetworkError)errorCode);
        }

        public override void OnStartHost() {
            // Log.Debug("%%% OnStartHost");
        }

        public override void OnStartServer() {
            // Log.Debug("%%% OnStartServer");
        }

        public override void OnStopServer() {
            //Log.Debug("Server has stopped");
        }

        public override void OnStopHost() {
            //Log.Debug("Host has stopped");
            if(state == DNMState.Host)
            {
                SetState(DNMState.Off);
            }
            else
            {
                Log.Warn("DNM: Unexpected call of OnStopHost");
            }
        }


        /////// CLIENT

        // Misc

        public void ClientSetReadyCommand(bool newValue)
        {
            if(state != DNMState.Host && state != DNMState.Client)
            {
                Log.Error("Invalid call of ClientSetReady: {0}", state);
                return;
            }

            if(gameState != GameState.NoGame)
            {
                Log.Error("Invalid call of ClientSetReady: {0}", gameState);
            }

            var readyMessage = new ReadyMessage(newValue);

            client.Send(MsgType.ClientSetReady, readyMessage);
        }

        // Client callbacks

        public override void OnStartClient(NetworkClient client) {
            // Log.Debug("%%% OnStartClient");
        }

        public override void OnStopClient() {
            // Log.Debug("%%% OnStopClient({0})", state);

            if(state == DNMState.Off)
            {
                // it was host and was stopped
                // it will be stopped OnStopHost
            }
            else if(state == DNMState.StartingAsClient)
            {
                // it was trying to connect as client and failed
                SetState(DNMState.Off);
            }
            else if(state == DNMState.Client)
            {
                // it was client and was stopped
                SetState(DNMState.Off);
            }
            else
            {
                Log.Warn("    with state {0}", state);
            }
        }

        public override void OnClientSceneChanged(NetworkConnection conn) {
            Log.Warn("######## OnClientSceneChanged called ### gameState={0}", gameState);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            // Log.Debug("### OnClientConnect({0}, {1}) srv:{2}", state.ToString(), conn.connectionId, NetworkServer.active ? "SI" : "no");

            if(state == DNMState.CreatingHost && NetworkServer.active)
            {
                SetState(DNMState.Host);
                RegisterServerHandlers();
                RegisterClientHandlers(conn);

                CreatePlayer(conn); // create hosted player
            }
            else if(state == DNMState.StartingAsClient && !NetworkServer.active)
            {
                conn.RegisterHandler(MsgType.InitialStatus, OnClientInitialStatusMessage);
                // wait for InitialStatus message
            }
            else
            {
                Log.Warn("Unexpected OnClientConnect case: {0}", state);
                return;
            }

            if(clientLoadedScene)
            {
                Log.Warn("Client already loaded scene");
                return;
            }
        }

        public override void OnClientDisconnect(NetworkConnection conn) {
            // Log.Debug("### OnClientDisconnect({0})", conn);

            StopClient();

            if(conn.lastError != NetworkError.Ok)
            {
                Log.Error("ClientDisconnected due to error: " + conn.lastError);
                //if (LogFilter.logError) { Debug.LogError("ClientDisconnected due to error: " + conn.lastError); }
            }
        }

        public override void OnClientError(NetworkConnection conn, int errorCode) {

            Log.Debug("Client network error occurred: " + (NetworkError)errorCode);

        }

        public override void OnClientNotReady(NetworkConnection conn) {

            Log.Debug("Server has set client to be not-ready (stop getting state updates)");

        }

        //////////////////////////

        // Instantiate client/server

        void InstantiateServer()
        {
            if(gameServer != null)
            {
                Log.Error("Already have a game server");
                return;
            }

            gameServer = Object.Instantiate(gameServerPrefab) as GameServer;
            
            Mode mode = Mode.OfflineMode;
            if(state == DNMState.Host)
            {
                mode = Mode.OnlineMode;
            }
            else if(state != DNMState.Offline)
            {
                Log.Error("Invalid state: {0}", state);
            }

            gameServer.StartServer(mode, levelData.MaxPlayers, playersPerRole);
        }

        delegate void OnClientInstantiated();

        // instantiates local client
        void InstantiateClientAsync(StartGameMessage message, OnClientInstantiated onFinishDelegate = null)
        {
            SetGameState(GameState.Preparing);
            LoadSceneAsync(message.scene, () =>
            {
                InstantiateClient(message);
                SetGameState(GameState.Playing);
                OnClientGameStarted(); // this is to hide game panel

                onFinishDelegate?.Invoke();
            });
        }

        void InstantiateClient(GameServer gameServer, Mode mode, int numRoles)
        {
            InstantiateClientObject();
            gameClient.StartClient(gameServer, mode, numRoles);
        }

        // instantiates remote client
        void InstantiateClient(StartGameMessage startGameMessage)
        {
            InstantiateClientObject();
            gameClient.StartClient(startGameMessage);
        }

        void InstantiateClientObject(GameServer gameServer = null)
        {
            if(gameClient != null)
            {
                Log.Error("Already have a game client");
                return;
            }

            gameClient = Object.Instantiate(gameClientPrefab) as GameClient;

            CheckState(new DNMState[] { DNMState.Offline, DNMState.Host, DNMState.Client });

            Mode mode = state == DNMState.Offline ? Mode.OfflineMode : Mode.OnlineMode;
            bool isHosted = state != DNMState.Client;
        }

        // only server to remotes
        void SendStatusToClient(bool accepted, NetworkConnection conn)
        {
            string sceneName = GetSceneName();
            var state = GameState.NoGame;
            MessageBase extraMessage = null;

            if(accepted)
            {
                state = gameState;

                if(gameState == GameState.NoGame || gameState == GameState.WillStart)
                {
                    // noop
                }
                else if(gameState == GameState.Playing || gameState == GameState.GameOver)
                {
                    if(gameServer == null)
                    {
                        Log.Error("No tengo server :(");
                    }
                    else
                    {
                        var initialMessages = new List<MessageBase>();
                        gameServer.WriteInitialData(initialMessages);
                        extraMessage = new StartGameMessage(sceneName, initialMessages);
                    }
                }
                else
                {
                    Log.Error("Unexpected state {0}...", gameState);
                }
            }

            conn.Send(MsgType.InitialStatus, new StatusMessage(accepted, sceneName, state, extraMessage));
        }

        // Server message handlers

        void OnServerClientSetReadyMessage(NetworkMessage messageReader)
        {
            ReadyMessage msg = messageReader.ReadMessage<ReadyMessage>();
            bool newReady = msg.value;

            NetworkConnection conn = messageReader.conn;

            List<PlayerController> controllers = conn.playerControllers;
            foreach(PlayerController c in controllers)
            {
                OnlinePlayer dnmPlayer = c.unetView.GetComponent<OnlinePlayer>();

                if(dnmPlayer.IsReady() == newReady)
                {
                    Log.Warn("Already in this ready state");
                }
                else
                {
                    dnmPlayer.SetReady(newReady);
                }
            }
        }

        void OnServerReadyToStartMessage(NetworkMessage messageReader)
        {
            if(gameState != GameState.Preparing)
            {
                Log.Warn("Unexpected game state in server A: {0}", gameState);
                return;
            }

            Client c = GetClient(messageReader.conn);
            if(c.stateInServer != GameState.NoGame)
            {
                Log.Warn("Unexpected client state in server B: {0}", c.stateInServer);
            }
            c.stateInServer = GameState.Preparing;

            if(AllClientsInState(GameState.Preparing))
            {
                StartGame();
            }
        }

        // Client message handlers

        // only in non-hosted clients
        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {
            if(NetworkServer.active)
            {
                Log.Error("Invalid in host");
                return;
            }

            StatusMessage msg = messageReader.ReadMessage<StatusMessage>();

            if(!msg.accepted)
            {
                Log.Warn("I was rejected :(");
                return;
            }

            SetGameState(msg.gameState);

            // TODO check game over case
            if(gameState == GameState.NoGame)
            {
                SetState(DNMState.Client);
                RegisterClientHandlers(messageReader.conn);
                CreatePlayer(localClient.connection); // create remote initial player
            }
            // TODO check this cases...
            else if(gameState == GameState.WillStart || gameState == GameState.Preparing || gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                // client is joining a started game

                SetState(DNMState.Client);
                RegisterClientHandlers(messageReader.conn);

                var startGameMessage = msg.ReadExtraMessage<StartGameMessage>();

                InstantiateClientAsync(startGameMessage, () =>
                {
                    CreatePlayer(localClient.connection); // create remote initial player
                });
            }
        }

        // create first local/remote client
        void CreatePlayer(NetworkConnection conn)
        {
            ClientScene.Ready(conn);
            var extraMessage = new CustomAddPlayerMessage(userManager.GetActiveUser());
            ClientScene.AddPlayer(null, 0, extraMessage);

            // TODO remove this!!!
            if(sceneToggle.isOn)
            {
                ClientScene.AddPlayer(null, 1, extraMessage);
            }
        }

        // doing nothing in hosted client
        void OnClientStartGameMessage(NetworkMessage messageReader)
        {
            if(state != DNMState.Host && state != DNMState.Client)
            {
                Log.Error("DNM: unexpected call of OnClientStartGameMessage: {0}", state);
                return;
            }

            bool hosted = state == DNMState.Host;

            if(!hosted)
            {
                var startGameMessage = messageReader.ReadMessage<StartGameMessage>();

                InstantiateClientAsync(startGameMessage, () =>
                {
                    client.Send(MsgType.ReadyToStart, new EmptyMessage());
                });
            }
            else
            {
                client.Send(MsgType.ReadyToStart, new EmptyMessage());
            }
        }

        void OnGameServerToClientMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<WrappedMessage>();

            if(!gameClient)
            {
                Log.Warn("OnGameClientMessage: no game client {0}", msg.messageType - MsgType.Highest);

                return;
            }

            gameClient.OnMessage(msg);
        }

        void OnGameClientToServerMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<WrappedMessage>();
            if(!gameServer)
            {
                Log.Warn("OnGameClientSendToServerMessage: no game server");
                return;
            }

            gameServer.OnMessage(msg, messageReader.conn.connectionId);
        }

        ///////////////////// State /////////////////////

        public DNMState GetState()
        {
            return state;
        }

        void SetState(DNMState newState)
        {
            if(state == newState)
            {
                Log.Warn("Repeated state");
                return;
            }

            state = newState;
            Info.Set("DNMState", state.ToString());
            OnStateChanged(state);
        }

        ///////////////////// Handlers /////////////////////

        void RegisterServerHandlers()
        {
            // client to mark itself as ready/non-ready to start playing
            NetworkServer.RegisterHandler(MsgType.ClientSetReady, OnServerClientSetReadyMessage);
            // client to mark itself as ready/non-ready to start playing
            NetworkServer.RegisterHandler(MsgType.ReadyToStart, OnServerReadyToStartMessage);
            // game-level message sent from client to server
            NetworkServer.RegisterHandler(MsgType.GameClientToServer, OnGameClientToServerMessage);
        }

        void RegisterClientHandlers(NetworkConnection conn)
        {
            conn.RegisterHandler(MsgType.StartGame, OnClientStartGameMessage);
            conn.RegisterHandler(MsgType.GameServerToClient, OnGameServerToClientMessage);
        }

        void SetGameState(GameState newState)
        {
            gameState = newState;
            Info.Set("GameState", gameState.ToString());
        }

        //// DNMListener

        public void AddListener(DNMListener listener)
        {
            listeners.Add(listener);
        }

        public void OnStateChanged(DNMState newState)
        {
            foreach(var l in listeners)
            {
                l.OnStateChanged(state);
            }
        }

        public void OnClientGameStarted()
        {
            foreach(DNMListener l in listeners)
            {
                l.OnClientGameStarted();
            }
        }

        // Server utils

        bool AllClientsInState(GameState state)
        {
            var clientList = new List<Client>(clients.Values);
            return clientList.TrueForAll(client => client.stateInServer == state);
        }

        Client GetClient(NetworkConnection conn)
        {
            return GetClient(conn.connectionId);
        }

        Client GetClient(int id)
        {
            if(!clients.ContainsKey(id))
            {
                Log.Error("Connection with id {0} not found", id);
                return null;
            }

            return clients[id];
        }

        int GetNextRole()
        {
            for(int i = 1; i <= levelData.MaxPlayers; i++)
            {
                var playerCount = NumberOfPlayersForRole(i);
                if(playerCount == 0)
                {
                    return i;
                }
            }

            return DNM.SpecRole;
        }

        int NumberOfPlayersForRole(int role)
        {
            if(role < DNM.FirstPlayerRole || role > levelData.MaxPlayers)
            {
                Log.Error("Invalid role number: {0}", role);
                return 0;
            }

            int ret = 0;

            foreach(Client client in clients.Values)
            {
                foreach(Player player in client.players)
                {
                    if(player.GetRole() == role)
                    {
                        ret++;
                    }
                }
            }

            return ret;
        }

        bool PlayersAreReady()
        {
            bool allReady = true;

            foreach(Client client in clients.Values)
            {
                foreach(DNMPlayer player in client.players)
                {
                    if(!player.IsSpectator() && !player.IsReady())
                    {
                        allReady = false;
                    }
                }
            }

            return allReady;
        }

        bool EnoughPlayersForEachRole()
        {
            bool enoughPlayers = true;

            for(int role = 1; role <= levelData.MaxPlayers; role++)
            {
                if(NumberOfPlayersForRole(role) < 1)
                {
                    enoughPlayers = false;
                    Log.Debug("Role {0} not satisfied", role);
                }
            }

            return enoughPlayers;
        }

        void CollectPlayers()
        {
            playersPerRole = new List<Player>[numRoles];

            foreach(Client client in clients.Values)
            {
                if(client.stateInServer != GameState.NoGame)
                {
                    Log.Warn("Unexpected state in server A: {0}", client.stateInServer);
                }
                client.stateInServer = GameState.NoGame;

                foreach(DNMPlayer player in client.players)
                {
                    if(!player.IsSpectator())
                    {
                        int role = player.GetRole();

                        if(playersPerRole[role - 1] == null)
                        {
                            playersPerRole[role - 1] = new List<Player>();
                        }
                        playersPerRole[role - 1].Add(player);
                    }
                }
            }
        }

        //// Messaging

        // server
        void SendToAll(short msgType, MessageBase message)
        {
            CheckState(DNMState.Host);

            NetworkServer.SendToAll(msgType, message);
        }

        //// Game-level messaging

        public void GameServerSendTo(int who, short msgType, MessageBase gameMessage)
        {
            CheckState(DNMState.Host);
            
            NetworkServer.SendToClient(who, MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
        }

        public void GameServerSendToAll(short msgType, MessageBase gameMessage)
        {
            CheckState(DNMState.Host);
            
            NetworkServer.SendToAll(MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
        }

        public void GameServerSendToAllBut(int who, short msgType, MessageBase gameMessage)
        {
            CheckState(DNMState.Host);
            
            foreach(var client in clients.Values)
            {
                int id = client.connection.connectionId;

                if(id != who && client.connection.isReady)
                {
                    NetworkServer.SendToClient(id, MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
                }
            }

        }

        public void GameClientSendToServer(short msgType, MessageBase message)
        {
            CheckState(new DNMState[] { DNMState.Host, DNMState.Client });
            
            client.Send(MsgType.GameClientToServer, new WrappedMessage(msgType, message));
        }

        // Checking

        void CheckState(DNMState expectedState)
        {
            if(state != expectedState)
            {
                WrongState(state.ToString());
            }
        }

        void CheckState(DNMState[] expectedStates)
        {
            if(!System.Array.Exists<DNMState>(expectedStates, s => s == state))
            {
                WrongState(state.ToString());
            }
        }

        void CheckGameState(GameState expectedState)
        {
            if(gameState != expectedState)
            {
                WrongState(gameState.ToString());
            }
        }

        void CheckGameState(GameState[] expectedStates)
        {
            if(!System.Array.Exists<GameState>(expectedStates, s => s == gameState))
            {
                WrongState(gameState.ToString());
            }
        }

        void WrongState(string stateString)
        {
            var errorMessage = System.String.Format("Unexpected state '{0}'", stateString);
            Log.Warn(errorMessage);
            throw new System.Exception(errorMessage);
        }

        // Scene loading

        delegate void OnFinishLoadingScene();

        void LoadSceneAsync(string sceneName, OnFinishLoadingScene onFinishDelegate)
        {

            StartCoroutine(LoadSceneCoroutine(sceneName, onFinishDelegate));


        }

        IEnumerator LoadSceneCoroutine(string sceneName, OnFinishLoadingScene onFinishDelegate)
        {

            var operation = SceneManager.LoadSceneAsync(sceneName);

            while(!operation.isDone)
            {
                yield return null; // wait for next frame
            }

            onFinishDelegate();
        }

        // ///////////////

        string GetSceneName()
        {
            return levelData.SceneName;
        }

    } // class DualNetworkManager

} // namespace Julo.Network
