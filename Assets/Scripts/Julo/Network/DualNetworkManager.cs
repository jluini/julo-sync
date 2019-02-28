using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

using Julo.Logging;
using Julo.Users;
using Julo.Game; // try not to use Game here

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
            
        // TODO add dedicated server mode
    }

    public class DualNetworkManager : NetworkManager
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
        public OfflineDualPlayer offlinePlayerModel;
        public OnlineDualPlayer onlinePlayerModel;

        [Header("Hooks")]
        public Transform playerContainer;
        public Toggle sceneToggle; // TODO remove 


        //[Header("Configuration")]
        //public GameServer gameServerPrefab;
        //public GameClient gameClientPrefab;

        ////////////////////////////////////////////////////////////

        public GameServer dualServer = null;
        GameClient dualClient = null;

        ////////////////////////////////////////////////////////////

        const int LocalConnectionId = 0;

        ////////////////////////////////////////////////////////////

        UserManager userManager = null;
        DNMState state = DNMState.NotInitialized;

        //GameState gameState = GameState.NoGame;
        //GameServer gameServer = null;
        //GameClient gameClient = null;

        NetworkClient localClient = null;

        //int numRoles;

        ////////////////////////////////////////////////////////////

        // only server
        //Dictionary<int, Client> clients;
        //List<Player>[] playersPerRole = null;

        ////////////////////////////////////////////////////////////

        // only offline mode

        uint lastOfflineIdUsed = 0;

        ////////////////////////////////////////////////////////////

        List<IDualListener> listeners = new List<IDualListener>();

        ////////////////////////////////////////////////////////////

        CreateServerDelegate serverDelegate;
        CreateHostedClientDelegate hostedClientDelegate;
        CreateRemoteClientDelegate remoteClientDelegate;

        public void Init(
            UserManager userManager,
            CreateServerDelegate serverDelegate,
            CreateHostedClientDelegate hostedClientDelegate,
            CreateRemoteClientDelegate remoteClientDelegate
        ) {
            Init(userManager);

            this.serverDelegate = serverDelegate;
            this.hostedClientDelegate = hostedClientDelegate;
            this.remoteClientDelegate = remoteClientDelegate;
        }

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
            //SetGameState(GameState.NoGame);
            this.userManager = userManager;
        }

        public void StartOffline()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM should be Off");
                return;
            }


            SetState(DNMState.Offline);
            /*
            clients = new Dictionary<int, Client>();
            var singleClient = new Client(null);
            clients.Add(LocalConnectionId, singleClient);
            */
            // TODO AddOfflinePlayer(userManager.GetActiveUser());

            // TODO delete this!!!
            //if(sceneToggle.isOn)
                //AddOfflinePlayer(userManager.GetActiveUser());

            ///
            
            // creates hosted offline server
            this.dualServer = (GameServer)serverDelegate(Mode.OfflineMode, hostedClientDelegate);
            this.dualClient = dualServer.localClient;
        }

        public void StartAsHost()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsHost");
                return;
            }

            //clients = new Dictionary<int, Client>();

            Log.Debug("DNMState.CreatingHost");
            SetState(DNMState.CreatingHost);

            // creates hosted online server
            dualServer = (GameServer) serverDelegate(Mode.OnlineMode, hostedClientDelegate);


            localClient = StartHost();

            if(localClient == null)
            {
                SetState(DNMState.Off);
                Log.Error("DNM: could not create host");
            }

            ///

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
                // TODO
                
                SetState(DNMState.Off);
                foreach(var p in dualServer.connections[LocalConnectionId].players)
                {
                    OfflineDualPlayer op = (OfflineDualPlayer)p;
                    Destroy(op.gameObject);
                }
                
                // TODO cleanup

                dualServer = null;
                dualClient = null;
                //clients = null;
            }
            else if(state == DNMState.Host)
            {
                // TODO destroy and clear things
                // TODO should clear clients ???????????????

                StopHost();

                dualServer = null;
                dualClient = null;
                //clients = null;
            }
            else if(state == DNMState.Client)
            {
                // TODO destroy and clear things

                StopClient();

                dualClient = null;
            }
            else
            {
                Log.Warn("DNM: unexpected call of Stop");
            }
        }

        ////////////////////////////////////////////////////////////

        //////////////////// Offline mode //////////////////////////
        /*
        void AddOfflinePlayer(UserProfile user)
        {
            var player = GameObject.Instantiate(offlinePlayerModel, playerContainer) as OfflineDualPlayer;
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
        public List<IDualPlayer> OfflinePlayers()
        {
            return GetClient(LocalConnectionId).players;
        }
        */

        ////////////////////////////////////////////////////////////

        /////// SERVER

        // Playing
        /*
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
            if(dualServer.playersPerRole != null)
            {
                Log.Warn("playersPerRole already initialized");
            }

            var sceneName = GetSceneName();

            SetGameState(GameState.Preparing);
            numRoles = levelData.MaxPlayers;
            CollectPlayers();

            //LoadSceneAsync(sceneName, () =>
            //{
                StartGame();

                /*
                InstantiateGameServer();

                // this is just to exclude DedicatedServer mode that doesn't exist yet
                if(state == DNMState.Offline)
                {
                    // instantiate local game client but no messaging
                    InstantiateGameClient(gameServer, Mode.OfflineMode, numRoles);
                    StartGame();
                    // TODO rest of things!
                }
                else if(state == DNMState.Host)
                {
                    // instantiate local client
                    InstantiateGameClient(gameServer, Mode.OnlineMode, numRoles);

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
                * /
            //});
        }
        
        void StartGame()
        {
            OnClientGameStarted(); // this is to hide game panel
            SetGameState(GameState.Playing);
            dualServer.StartGame(/*levelData.MaxPlayers, playersPerRole* /);
        }
        */

        public void ChangeRole(IDualPlayer player)
        {

            Log.Warn("ChangeRole not implemented");
            /*
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
        
        // Server callbacks

        public override void OnServerConnect(NetworkConnection conn)
        {
            Log.Debug("### OnServerConnect({0}, {1})", state.ToString(), conn.connectionId);

            CheckState(new DNMState[] { DNMState.CreatingHost, DNMState.Host });

            dualServer.OnConnect(conn);

            /*
            int id = conn.connectionId;
            if(dualServer.connections.ContainsKey(id))
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
            */
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Log.Debug("### OnServerDisconnect({0})", conn.connectionId);

            // TODO avoid warnings for rejected clients

            // TODO pasar a DualServer en vez de violar encapsulamiento
            
            CheckState(DNMState.Host);

            NetworkServer.DestroyPlayersForConnection(conn);

            var id = conn.connectionId;

            if(dualServer.connections.ContainsKey(id))
            {
                dualServer.connections.Remove(id);
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

            //throw new System.NotImplementedException();

            
            /*
            CustomAddPlayerMessage msg = messageReader.ReadMessage<CustomAddPlayerMessage>();

            string username = msg.username;

            var player = GameObject.Instantiate(onlinePlayerModel/*, playerContainer not needed * /) as OnlinePlayer;
            UserProfile user = new UserProfile(username);
            int role = DNM.SpecRole;
            */
            /*
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

            dualServer.connections[conn.connectionId].AddPlayer(player);
            //GetClient(conn).AddPlayer(player);

            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
            */
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
            Log.Warn("ClientSetReadyCommand not implemented");
            /*
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

            client.Send(MsgType.ClientSetReady, readyMessage);*/
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
            Log.Warn("######## OnClientSceneChanged called ### state={0}", state);
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
                // Waiting for InitialStatus message
                Log.Debug("Waiting for InitialStatus message");
            }
            else
            {
                Log.Warn("Unexpected OnClientConnect case: {0}, {1}", state, NetworkServer.active);
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
        /*
        void InstantiateGameServer()
        {

            ///

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
        */

        delegate void OnClientInstantiated();
        
        /*
        // instantiates local client
        void InstantiateGameClient(GameServer gameServer, Mode mode, int numRoles)
        {
            InstantiateClientObject();
            gameClient.StartClient(gameServer, mode, numRoles);
        }
        */
        /*
        // instantiates remote client
        void InstantiateClientAsync(StartRemoteClientMessage message, OnClientInstantiated onFinishDelegate = null)
        {
            //var sceneName = message.sceneName;
            //Log.Debug("Loading scene '{0}'", sceneName);

            //SetGameState(GameState.Preparing);
            LoadSceneAsync(sceneName, () =>
            {
                //InstantiateGameClient(message);
                // creates non-hosted client
                dualClient = (GameClient)remoteClientDelegate(message);


                SetGameState(GameState.Playing);
                OnClientGameStarted(); // this is to hide game panel

                onFinishDelegate?.Invoke();
            });
        }
        */
        /*
        // instantiates remote client
        void InstantiateGameClient(StartRemoteClientMessage startClientMessage)
        {
            // creates non-hosted client
            dualClient = (GameClient) remoteClientDelegate(startClientMessage);
        }

        // only server to remotes
        void SendStatusToClient(bool accepted, NetworkConnection conn)
        {
            Log.Debug("Sending InitialStatus message!!!");
            string sceneName = GetSceneName();
            //MessageBase extraMessage = null;
            var initialMessages = new List<MessageBase>();

            if(accepted)
            {
                if(dualServer == null)
                {
                    Log.Error("No tengo server :(");
                }
                else
                {
                    dualServer.WriteRemoteClientData(initialMessages);
                    //extraMessage = new StartGameMessage(sceneName, initialMessages);
                }

            }

            Log.Debug("Sending InitialStatus message: {0}, {1}, {2}", accepted, sceneName, initialMessages.Count);
            conn.Send(MsgType.InitialStatus, new StartRemoteClientMessage(accepted, sceneName, initialMessages));
        }
        */
        // Server message handlers
        /*
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
        */
        /*
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
        */
        // Client message handlers

        // only in non-hosted clients
        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {
            // TODO pasar a DualClient?

            // TODO necessary to receive if rejected?

            CheckState(DNMState.StartingAsClient);

            var msg = messageReader.ReadMessage<StartRemoteClientMessage>();

            Log.Debug("Received InitialStatus message: {0}, {1}, {2}", msg.accepted, msg.count);

            if(!msg.accepted)
            {
                Log.Warn("I was rejected :(");
                return;
            }

            // creates non-hosted client
            dualClient = (GameClient)remoteClientDelegate(msg);


            SetState(DNMState.Client);
            CreatePlayer(localClient.connection); // create remote initial player

            /*
            InstantiateClientAsync(msg, () =>
            {
                SetState(DNMState.Client);
                CreatePlayer(localClient.connection); // create remote initial player
            });
            */
        }

        // create first local/remote client
        void CreatePlayer(NetworkConnection conn)
        {
            // TODO set ready here?

            ClientScene.Ready(conn);
            var extraMessage = new CustomAddPlayerMessage(userManager.GetActiveUser());
            ClientScene.AddPlayer(null, 0, extraMessage);

            // TODO remove this!!!
            if(sceneToggle.isOn)
            {
                ClientScene.AddPlayer(null, 1, extraMessage);
            }
        }
        
        /*
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
        */

        void OnGameServerToClientMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<WrappedMessage>();

            if(dualClient == null)
            {
                Log.Warn("OnGameClientMessage: no game client {0}", msg.messageType - MsgType.Highest);

                return;
            }

            dualClient.SendMessage(msg);
        }

        void OnGameClientToServerMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<WrappedMessage>();
            if(dualServer == null)
            {
                Log.Warn("OnGameClientSendToServerMessage: no game server");
                return;
            }

            dualServer.SendMessage(msg, messageReader.conn.connectionId);
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
            foreach(var l in listeners)
            {
                l.OnStateChanged(state);
            }
        }

        ///////////////////// Handlers /////////////////////

        void RegisterServerHandlers()
        {
            // client to mark itself as ready/non-ready to start playing
            //NetworkServer.RegisterHandler(MsgType.ClientSetReady, OnServerClientSetReadyMessage);
            // client to mark itself as ready/non-ready to start playing
            //NetworkServer.RegisterHandler(MsgType.ReadyToStart, OnServerReadyToStartMessage);
            // game-level message sent from client to server
            NetworkServer.RegisterHandler(MsgType.GameClientToServer, OnGameClientToServerMessage);
        }

        void RegisterClientHandlers(NetworkConnection conn)
        {
            //conn.RegisterHandler(MsgType.StartGame, OnClientStartGameMessage);
            conn.RegisterHandler(MsgType.GameServerToClient, OnGameServerToClientMessage);
        }
        /*
        void SetGameState(GameState newState)
        {
            gameState = newState;
            Info.Set("GameState", gameState.ToString());
        }
        */
        //// IDualListener

        public void AddListener(IDualListener listener)
        {
            listeners.Add(listener);
        }
        /*
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
        */
        // Server utils
        /*
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
                foreach(IDualPlayer player in client.players)
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

                foreach(IDualPlayer player in client.players)
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
        */

        //// Messaging

        // server
        void SendToAll(short msgType, MessageBase message)
        {
            CheckState(DNMState.Host);

            NetworkServer.SendToAll(msgType, message);
        }

        // TODO no longer call this "game" level
        //// Game-level messaging

        // TODO remove "game" word
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

        // TODO remove "game" word
        public void GameServerSendToAllBut(int who, short msgType, MessageBase gameMessage)
        {
            CheckState(DNMState.Host);
            
            //foreach(var client in clients.Values)
            foreach(var c in dualServer.connections.Values) // TODO TODO TODO TODO TODO 
            {
                int id = c.ConnectionId();

                if(id != who && c.networkConnection.isReady)
                {
                    NetworkServer.SendToClient(id, MsgType.GameServerToClient, new WrappedMessage(msgType, gameMessage));
                }
            }

        }

        // TODO remove "game" word
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
        /*
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
        */
        void WrongState(string stateString)
        {
            var errorMessage = System.String.Format("Unexpected state '{0}'", stateString);
            Log.Warn(errorMessage);
            throw new System.Exception(errorMessage);
        }

        // Scene loading

        public delegate void OnFinishLoadingScene();

        public void LoadSceneAsync(string sceneName, OnFinishLoadingScene onFinishDelegate)
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
