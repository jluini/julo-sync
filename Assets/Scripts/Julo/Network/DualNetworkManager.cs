using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

using Julo.Logging;
using Julo.Users;
using Julo.Panels;

namespace Julo.Network
{

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

        // TODO rename to mode
        public enum DNMState
        {
            NotInitialized,
            Off,
            Offline,
            Host,
            Client
            
            // TODO add dedicated server mode
        }

        public enum GameState
        {

            NoGame, // only when not initialized
            Lobby,  // lobby as server or client

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
        DNMState currentState = DNMState.NotInitialized;

        GameState gameState = GameState.NoGame;
        GameServer gameServer = null;
        GameClient gameClient = null;

        NetworkClient localClient = null;

        ////////////////////////////////////////////////////////////

        // only server
        Dictionary<int, Client> clients;
        List<Player>[] playersPerRole = null;
        bool sceneHasStarted = true;
        //bool allClientsAreReadyToSpawn = true;

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

            if(currentState != DNMState.NotInitialized)
            {
                Log.Error("DNM already initialized");
                return;
            }
            
            SetState(DNMState.Off);
            SetGameState(GameState.NoGame);
            this.userManager = userManager;

            // client to server when first connecting
            NetworkServer.RegisterHandler(MsgType.StatusRequest, OnServerStatusRequestMessage);
            
            // client to mark itself as ready/non-ready to start playing
            NetworkServer.RegisterHandler(MsgType.ClientSetReady, OnServerClientSetReadyMessage);

            // client to mark itself as ready/non-ready to start playing
            NetworkServer.RegisterHandler(MsgType.ReadyToSpawn, OnServerReadyToSpawnMessage);
            
            NetworkServer.RegisterHandler(MsgType.GameClientToServer, OnGameClientToServerMessage);
        }
        
        public void StartOffline()
        {
            if(currentState != DNMState.Off)
            {
                Log.Error("DNM should be Off");
                return;
            }

            Log.Debug("START OFFLINE: " + userManager.GetActiveUser().GetName());

            SetState(DNMState.Offline);

            // TODO create dict here?
            clients = new Dictionary<int, Client>();

            var singleClient = new Client(null);
            clients.Add(LocalConnectionId, singleClient);

            AddOfflinePlayer(userManager.GetActiveUser());
        }

        public bool StartAsHost()
        {
            if(currentState != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsHost");
                return false;
            }

            Log.Debug(System.String.Format("DNM: starting host for '{0}'", userManager.GetActiveUser().GetName()));

            clients = new Dictionary<int, Client>(); // TODO start dict here?

            localClient = StartHost();

            if(localClient != null)
            {
                // host started

                SetState(DNMState.Host);
                SetGameState(GameState.Lobby);

                return true;
            }
            else
            {
                Log.Error("DNM: could not create host");
                return false;
            }
        }

        public bool StartAsClient()
        {
            if(currentState != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsClient");
                return false;
            }

            localClient = StartClient();

            if(localClient != null)
            {
                // client started
                SetState(DNMState.Client);
                
                return true;
            }
            else
            {
                Log.Error("DNM: could not start client");
                return false;
            }
        }

        ////////////////////////////////////////////////////////////

        bool AddOfflinePlayer(UserProfile user)
        {
            Log.Warn("DualNetworkManager::AddOfflinePlayer to be implemented");
            throw new System.NotImplementedException();
            // return true;
        }

        ////////////////////////////////////////////////////////////

        /////// SERVER

        // Misc

        public void TryToStartGame()
        {
            if(currentState != DNMState.Host)
            {
                Log.Error("Invalid call of StartGame: {0}", currentState);
                return;
            }
            if(gameState != GameState.Lobby)
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

            SetGameState(GameState.Preparing);

            // TODO variable number of players?
            playersPerRole = new List<Player>[levelData.MaxPlayers];

            // TODO ignore non-playing clients!!!
            foreach(Client client in clients.Values)
            {
                if(client.stateInServer != GameState.Lobby)
                {
                    Log.Warn("Unexpected state in server A: {0}", client.stateInServer);
                }
                client.stateInServer = GameState.Lobby; // TODO will start?

                foreach(DNMPlayer player in client.players)
                {
                    if(!player.IsSpectator())
                    {
                        int roleIndex = player.GetRole() - 1;

                        if(playersPerRole[roleIndex] == null)
                        {
                            playersPerRole[roleIndex] = new List<Player>();
                        }
                        playersPerRole[roleIndex].Add(player);
                    }
                }
            }

            sceneHasStarted = false;
            SceneManager.LoadScene("beach"); // TODO hardcoded map

            // TODO do this delayed or do server DontDestroyOnLoad?
            //InstantiateServerDelayed();
            InstantiateServer();

            if(GetState() == DNMState.Offline)
            {
                // should send a "mock message" to create offline local client?
                throw new System.NotImplementedException();
            }
            else
            {
                NetworkServer.SendToAll(MsgType.Prepare, new StringMessage("beach")); // TODO map
            }
        }
        
        public void ChangeRole(OnlinePlayer player)
        {
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
                    newRole = DNM.SpecRole;
            }

            Log.Info(System.String.Format("{0} -> {1}", currentRole, newRole));

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
            //Log.Debug("### OnServerConnect({0})", conn.connectionId);

            int id = conn.connectionId;

            if(clients.ContainsKey(id))
            {
                Log.Error("Client already registered");
                return;
            }

            var client = new Client(conn);

            clients.Add(id, client);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            NetworkServer.DestroyPlayersForConnection(conn);

            if (conn.lastError != NetworkError.Ok)
            {
                Log.Error("ServerDisconnected due to error: " + conn.lastError);
                // if (LogFilter.logError) { Debug.LogError("ServerDisconnected due to error: " + conn.lastError); }
            }

            Log.Debug("A client disconnected from the server: " + conn);
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            NetworkServer.SetClientReady(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId, NetworkReader messageReader)
        {
            CustomAddPlayerMessage msg = messageReader.ReadMessage<CustomAddPlayerMessage>();

            string username = msg.username;

            var player = GameObject.Instantiate(onlinePlayerModel/*, playerContainer not needed */) as OnlinePlayer;

            UserProfile user = new UserProfile(username);
            int role = DNM.SpecRole;

            if(gameState == GameState.Lobby)
            {
                // TODO check way of deciding initial role
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

            GetConnection(conn).AddPlayer(player);

            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
            Log.Error("OnServerAddPlayer should not be called without extra message");
        }

        public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player) {
            if (player.gameObject != null)
                NetworkServer.Destroy(player.gameObject);
        }

        public override void OnServerError(NetworkConnection conn, int errorCode) {
            Log.Debug("Server network error occurred: " + (NetworkError)errorCode);
        }

        public override void OnStartHost() {
            //Log.Debug("Host has started");
        }

        public override void OnStartServer() {
            //Log.Debug("Server has started");
        }

        public override void OnStopServer() {
            //Log.Debug("Server has stopped");
        }

        public override void OnStopHost() {
            //Log.Debug("Host has stopped");
        }


        /////// CLIENT

        // Misc

        public void ClientSetReadyCommand(bool newValue)
        {
            if(currentState != DNMState.Host && currentState != DNMState.Client)
            {
                Log.Error("Invalid call of ClientSetReady: {0}", currentState);
                return;
            }

            if(gameState != GameState.Lobby)
            {
                Log.Error("Invalid call of ClientSetReady: {0}", gameState);
            }

            var readyMessage = new ReadyMessage(newValue);

            client.Send(MsgType.ClientSetReady, readyMessage);
        }
        
        // Client callbacks

        public override void OnClientConnect(NetworkConnection conn)
        {
            conn.RegisterHandler(MsgType.InitialStatus, OnClientInitialStatusMessage);
            conn.RegisterHandler(MsgType.Prepare, OnClientPrepareMessage);
            conn.RegisterHandler(MsgType.GameServerToClient, OnGameServerToClientMessage);

            if(clientLoadedScene)
            {
                Log.Warn("Client already loaded scene");
                return;
            }

            client.Send(MsgType.StatusRequest, new EmptyMessage());
        }
        
        public override void OnClientDisconnect(NetworkConnection conn) {

            StopClient();

            if (conn.lastError != NetworkError.Ok)
            {
                Log.Error("ClientDisconnected due to error: " + conn.lastError);
                //if (LogFilter.logError) { Debug.LogError("ClientDisconnected due to error: " + conn.lastError); }
            }

            Log.Debug("Client disconnected from server: " + conn);

        }

        public override void OnClientError(NetworkConnection conn, int errorCode) {

            Log.Debug("Client network error occurred: " + (NetworkError)errorCode);

        }

        public override void OnClientNotReady(NetworkConnection conn) {

            Log.Debug("Server has set client to be not-ready (stop getting state updates)");

        }

        public override void OnStartClient(NetworkClient client) {
            //Log.Debug("### OnStartClient");
            OnClientStarted();
        }

        public override void OnStopClient() {
            Log.Debug("Client has stopped");
        }

        public override void OnClientSceneChanged(NetworkConnection conn) {
            // TODO is this called?

            Log.Debug("### OnClientSceneChanged ### gameState={0}", gameState);
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
            Log.Debug("Instantiating game server");

            gameServer = Object.Instantiate(gameServerPrefab) as GameServer;
            
            DontDestroyOnLoad(gameServer.gameObject); // TODO check if can be avoided

            Mode mode = Mode.OfflineMode;
            if(currentState == DNMState.Host)
            {
                mode = Mode.OnlineMode;
            }
            else if(currentState != DNMState.Offline)
            {
                Log.Error("Invalid state: {0}", currentState);
            }

            gameServer.StartServer(mode, levelData.MaxPlayers, playersPerRole);
        }

        void InstantiateClient()
        {
            if(gameClient != null)
            {
                Log.Error("Already have a game client");
                return;
            }

            gameClient = Object.Instantiate(gameClientPrefab) as GameClient;

            DontDestroyOnLoad(gameClient.gameObject); // TODO check if can be avoided

            Mode mode = Mode.OfflineMode;
            bool isHosted = false;

            // TODO delete this if!!!
            if(currentState == DNMState.Offline)
            {
                mode = Mode.OfflineMode;
                isHosted = true;
            }
            else if(currentState == DNMState.Host)
            {
                mode = Mode.OnlineMode;
                isHosted = true;
            }
            else if(currentState == DNMState.Client)
            {
                mode = Mode.OnlineMode;
                isHosted = false;
            }
            else
            {
                Log.Error("Unexpected state: {0}", currentState);
                return;
            }

            gameClient.StartClient(mode, isHosted, levelData.MaxPlayers);
        }

        // Server message handlers

        void OnServerStatusRequestMessage(NetworkMessage messageReader)
        {
            NetworkConnection conn = messageReader.conn;

            MessageBase extraMessage = null;

            if(gameState == GameState.Lobby || gameState == GameState.WillStart)
            {
                extraMessage = null;
            }
            else if(gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                if(gameServer == null)
                {
                    Log.Error("No tengo server :(");
                }
                else
                {
                    // TODO
                    extraMessage = gameServer.GetStateMessage();
                }
            }
            else
            {
                Log.Error("Unexpected state {0}...", gameState);
            }

            conn.Send(MsgType.InitialStatus, new StatusMessage("beach", gameState, extraMessage));
        }

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
        void OnServerReadyToSpawnMessage(NetworkMessage messageReader)
        {
            if(gameState != GameState.Preparing)
            {
                Log.Warn("Unexpected game state in server A: {0}", gameState);
                return;
            }

            Client c = GetConnection(messageReader.conn);
            if(c.stateInServer != GameState.Lobby)
            {
                Log.Warn("Unexpected client state in server B: {0}", c.stateInServer);
            }
            c.stateInServer = GameState.Preparing;

            if(AllClientsInState(GameState.Preparing))
            {
                // TODO remove check?
                if(playersPerRole == null || playersPerRole.Length != levelData.MaxPlayers)
                {
                    Log.Error("Invalid players per role");
                }

                SetGameState(GameState.Playing);
                StartCoroutine(StartGameDelayed());

                //allClientsAreReadyToSpawn = true;
                /*
                if(sceneHasStarted)
                {
                    Log.Debug("Spawning because of all clients getting ready to spawn");
                    Spawn();
                }
                */
                /*
                SetGameState(GameState.Spawning);
                gameServer.SpawnInitialUnits();
                //StartCoroutine(StartGameDelayed());
                */
            }
        }

        IEnumerator StartGameDelayed()
        {
            yield return new WaitForSecondsRealtime(1f);
            gameServer.StartGame();

            // TODO should do this after a spawning confirmation?
            //NetworkServer.SendToAll(MsgType.GameStarted, gameServer.GetStateMessage());
            // TODO should do this after a GameStarted confirmation?
            //gameServer.StartGame();
        }
        // Client message handlers

        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {

            StatusMessage msg = messageReader.ReadMessage<StatusMessage>();

            SetGameState(msg.gameState);

            // TODO check game over case
            if(gameState == GameState.NoGame)
            {
                Log.Error("Unexpected NoGame");
                return;
            }
            else if(gameState == GameState.WillStart || gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                // client is joining a started game

                SceneManager.LoadScene(msg.map);

                // InstantiateClient(); TODO

                if(gameState != GameState.WillStart)
                {
                    // read extra message
                    //var extraMsg = msg.ReadExtraMessage<UnityEngine.Networking.NetworkSystem.StringMessage>();
                    gameClient.LateJoinGame(msg.ExtraReader());
                }
            }

            OnClientInitialStatus(msg.map, msg.gameState);


            // TODO do this always?
            ClientScene.Ready(localClient.connection);
            var extraMessage = new CustomAddPlayerMessage(userManager.GetActiveUser());
            ClientScene.AddPlayer(null, 0, extraMessage);
            
            // TODO remove this!!!
            if(sceneToggle.isOn)
            {
                ClientScene.AddPlayer(null, 1, extraMessage);
            }
        }
        
        void OnClientPrepareMessage(NetworkMessage messageReader)
        {
            if(GetState() == DNMState.Offline)
            {
                throw new System.NotImplementedException();
            }

            bool hosted = GetState() == DNMState.Host;
            if(hosted)
            {
                if(gameState != GameState.Preparing)
                {
                    Log.Error("Unexpected message Prepare in host: {0}", gameState);
                }
            }
            else
            {
                if(gameState != GameState.Lobby)
                {
                    Log.Error("Unexpected message Prepare in remote client: {0}", gameState);
                }

                SetGameState(GameState.Preparing);
            }

            //PrepareMessage msg = messageReader.ReadMessage<PrepareMessage>();
            var msg = messageReader.ReadMessage<StringMessage>();

            string clientMap = msg.value;
            //NetworkReader clientInitialStateReader = msg.ExtraReader();

            if(!hosted)
            {
                sceneHasStarted = false; // TODO needed?
                SceneManager.LoadScene(clientMap);
            }

            InstantiateClient();

            // this is to hide game panel
            OnClientGameStarted(); // TODO do this delayed?

            client.Send(MsgType.ReadyToSpawn, new EmptyMessage());
        }
        
        void OnGameServerToClientMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<WrappedMessage>();

            if(!gameClient)
            {
                Log.Warn("OnGameClientMessage: no game client");
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

        ////////////////////////////////////////////////////////////

        public DNMState GetState()
        {
            return currentState;
        }

        void SetState(DNMState newState)
        {
            currentState = newState;
            Info.Set("DNMState", currentState.ToString());
        }

        void SetGameState(GameState newState)
        {
            gameState = newState;
            Info.Set("GameState", gameState.ToString());
        }

        // Listening

        public void AddListener(DNMListener listener)
        {
            listeners.Add(listener);
        }
        
        public void OnClientStarted()
        {
            foreach(DNMListener l in listeners)
            {
                l.OnClientStarted();
            }
        }

        public void OnClientInitialStatus(string map, DualNetworkManager.GameState state)
        {
            foreach(DNMListener l in listeners)
            {
                l.OnClientInitialStatus(map, state);
            }
        }

        public void OnClientGameWillStart(string map)
        {
            foreach(DNMListener l in listeners)
            {
                l.OnClientGameWillStart(map);
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
            bool ret = true;

            foreach(Client client in clients.Values)
            {
                if(client.stateInServer != state)
                {
                    ret = false;
                    break;
                }
            }

            return ret;
        }

        Client GetConnection(NetworkConnection conn)
        {
            int id = conn.connectionId;

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
                if(NumberOfPlayersForRole(i) == 0)
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

        //// Messaging
        
        public void GameServerSendToAll(short msgType, MessageBase gameMessage)
        {
            // TODO checks
            NetworkServer.SendToAll(MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
        }

        public void GameServerSendToAllBut(int who, short msgType, MessageBase gameMessage)
        {
            // TODO checks
            //NetworkServer.SendToAll(MsgType.GameServerSendToAll, new WrappedMessage(msgType, message));
            
            foreach(int id in clients.Keys)
            {
                if(id != who)
                {
                    NetworkServer.SendToClient(id, MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
                }
            }

        }

        public void GameClientSendToServer(short msgType, MessageBase message)
        {
            // TODO checks
            client.Send(MsgType.GameClientToServer, new WrappedMessage(msgType, message));
        }


    } // class DualNetworkManager

} // namespace Julo.Network
