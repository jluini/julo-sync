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

            NoGame,
            Lobby,
            WillStart,
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

        GameServer gameServer;
        GameClient gameClient;

        ////////////////////////////////////////////////////////////

        DNMState currentState = DNMState.NotInitialized;
        GameState gameState = GameState.NoGame;

        const int LocalConnectionId = 0;

        UserManager userManager = null;


        ////////////////////////////////////////////////////////////

        // only server
        Dictionary<int, Client> clients;
        NetworkClient localClient = null;
        List<Player>[] playersPerRole = null;

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

            // TODO set here?
            NetworkServer.RegisterHandler(MsgType.StatusRequest, OnServerStatusRequestMessage);
            NetworkServer.RegisterHandler(MsgType.ClientSetReady, OnServerClientSetReadyMessage);
            NetworkServer.RegisterHandler(MsgType.ReadyToStart, OnServerReadyToStartMessage);
        }

        void OnServerStatusRequestMessage(NetworkMessage messageReader)
        {
            NetworkConnection conn = messageReader.conn;
            Log.Debug("### StatusRequest({0})", conn.connectionId);

            MessageBase extraMessage = null;

            if(gameState == GameState.Lobby || gameState == GameState.WillStart)
            {
                extraMessage = null;
            }
            else if (gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                if(gameServer == null)
                {
                    Log.Error("No tengo server :(");
                }
                else
                {
                    extraMessage = gameServer.GetStatusMessage();
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

        void OnServerReadyToStartMessage(NetworkMessage messageReader)
        {
            NetworkConnection conn = messageReader.conn;
            Client c = clients[conn.connectionId];

            if(c.IsReadyToStart())
            {
                Log.Error("Already was ready :(");
            }
            else
            {
                c.SetReadyToStart(true);
            }

            if(gameState != GameState.WillStart)
            {
                Log.Warn("Unexpected game state: {0}", gameState);
                return;
            }
            
            bool allReady = true;

            foreach(Client client in clients.Values)
            {
                if(!client.IsReadyToStart())
                {
                    allReady = false;
                    break;
                }
            }

            if(allReady)
            {
                Log.Debug("All are ready!!!");

                if(playersPerRole == null || playersPerRole.Length != levelData.MaxPlayers)
                {
                    Log.Error("Invalid players per role");
                }

                gameServer = Object.Instantiate(gameServerPrefab) as GameServer;

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

                SetGameState(GameState.Playing);
                NetworkServer.SendToAll(MsgType.GameStarted, gameServer.GetStatusMessage());
            }
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

        // Server callbacks

        public override void OnServerConnect(NetworkConnection conn)
        {
            Log.Debug("### OnServerConnect({0})", conn.connectionId);

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

            //players.Add(player);
            GetConnection(conn).AddPlayer(player);

            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
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

        // Client callbacks

        public override void OnClientConnect(NetworkConnection conn)
        {
            Log.Debug("### OnClientConnect({0})", conn.connectionId);
            //Log.Info(System.String.Format("OnClientConnect({0}) ({1}:{2})", conn, currentState, NetworkServer.active ? "HOSTED" : "REMOTE"));

            conn.RegisterHandler(MsgType.InitialStatus, OnClientInitialStatusMessage);
            conn.RegisterHandler(MsgType.GameWillStart, OnClientGameWillStartMessage);
            conn.RegisterHandler(MsgType.GameStarted, OnClientGameStartedMessage);

            if(clientLoadedScene)
            {
                Log.Warn("Client already loaded scene");
                return;
            }

            client.Send(MsgType.StatusRequest, new EmptyMessage());
        }

        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {

        //    StartCoroutine(OnClientInitialStatusMessage2(messageReader));
        //}
        //IEnumerator OnClientInitialStatusMessage2(NetworkMessage messageReader)
        //{
            StatusMessage msg = messageReader.ReadMessage<StatusMessage>();

            Log.Debug("### InitialStatus({0}, {1})", messageReader.conn.connectionId, msg.ToString());

            SetGameState(msg.gameState);

            // TODO check game over case
            if (gameState == GameState.NoGame)
            {
                Log.Error("Unexpected NoGame");
                return;
            }
            else if(gameState == GameState.WillStart || gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                // client is joining a started game

                // TODO delete this check!!
                if(sceneToggle.isOn)
                {
                    SceneManager.LoadScene(msg.map);
                }

                InstantiateClient();

                if(gameState != GameState.WillStart)
                {
                    // read extra message
                    //var extraMsg = msg.ReadExtraMessage<UnityEngine.Networking.NetworkSystem.StringMessage>();
                    gameClient.LateJoinGame(msg.ExtraReader());
                }
            }

            OnClientInitialStatus(msg.map, msg.gameState);

            //yield return new WaitForSecondsRealtime(2f);

            // TODO do this always?
            ClientScene.Ready(localClient.connection);
            var extraMessage = new CustomAddPlayerMessage(userManager.GetActiveUser());
            ClientScene.AddPlayer(null, 0, extraMessage);

            //yield return null;
        }

        void OnClientGameWillStartMessage(NetworkMessage messageReader)
        {
            StatusMessage msg = messageReader.ReadMessage<StatusMessage>();

            SetGameState(GameState.WillStart);
            
            // TODO delete this check!!
            if (sceneToggle.isOn)
            {
                SceneManager.LoadScene(msg.map);
            }

            OnClientGameWillStart(msg.map);

            client.Send(MsgType.ReadyToStart, new EmptyMessage());
        }

        void OnClientGameStartedMessage(NetworkMessage message)
        {
            SetGameState(GameState.Playing);
            Log.Debug("GAME STARTED");

            InstantiateClient(); // TODO do this on willStart?

            gameClient.StartGame(message.reader);

            OnClientGameStarted();
        }

        void InstantiateClient()
        {
            if (gameClient != null)
            {
                Log.Error("Already have a client");
                return;
            }

            gameClient = Object.Instantiate(gameClientPrefab) as GameClient;

            Mode mode = Mode.OfflineMode;
            bool isHosted = false;

            if (currentState == DNMState.Offline)
            {
                mode = Mode.OfflineMode;
                isHosted = true;
            }
            else if (currentState == DNMState.Host)
            {
                mode = Mode.OnlineMode;
                isHosted = true;
            }
            else if (currentState == DNMState.Client)
            {
                mode = Mode.OnlineMode;
                isHosted = false;
            }
            else
            {
                Log.Error("Unexpected state: {0}", currentState);
                return;
            }

            // TODO synchronize number of roles
            gameClient.StartClient(mode, isHosted, levelData.MaxPlayers);

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

        ///

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

        public void ClientSetReady(bool newValue)
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

            StartGame();
        }
        
        void StartGame()
        {
            if(playersPerRole != null)
            {
                Log.Warn("playersPerRole already initialized");
            }

            // TODO variable number of players?
            playersPerRole = new List<Player>[levelData.MaxPlayers];

            foreach(Client client in clients.Values)
            {
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

            // initiates scene change; when all playing connections send ReadyToStart it will actually start
            NetworkServer.SendToAll(MsgType.GameWillStart, new StatusMessage("beach", GameState.WillStart, null));
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

        ////////////////////////////////////////////////////////////

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

    } // class DualNetworkManager

} // namespace Julo.Network
