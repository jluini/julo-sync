using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.SceneManagement;

using Julo.Logging;
using Julo.Users;
//using Julo.Game; // try not to use Game here

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
        WaitingAcceptanceAsClient, // TODO change name
        WaitingInitialStateAsClient, // TODO change name?
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

        //GameServer dualServer = null;
        //GameClient dualClient = null;
        DualServer dualServer = null;
        DualClient dualClient = null;

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

        Dictionary<NetworkConnection, bool> acceptedConnections = new Dictionary<NetworkConnection, bool>();

        ConnectionToClient serverToLocalClientConnection = null;

        ////////////////////////////////////////////////////////////

        // only offline mode

        int lastOfflineIdUsed = 0;

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
            
            // creates hosted offline server
            dualServer = serverDelegate(Mode.OfflineMode);
            
            dualClient = hostedClientDelegate(Mode.OfflineMode, dualServer);

            serverToLocalClientConnection = new ConnectionToClient(null);
            //dualServer.AddClient(DNM.LocalConnectionId, connection);
            dualServer.AddLocalClient(dualClient, serverToLocalClientConnection);

            AddOfflinePlayer(0);
            if(sceneToggle.isOn) // TODO remove this!!!
            {
                AddOfflinePlayer(1);
            }

        }

        void AddOfflinePlayer(int controllerId)
        {
            var player = GameObject.Instantiate(offlinePlayerModel, playerContainer) as OfflineDualPlayer;
            player.Init(++lastOfflineIdUsed);
            /*var messageStack = */dualServer.AddPlayer(player);

            // TODO discarding messageStack
        }

        public void StartAsHost()
        {
            if(state != DNMState.Off)
            {
                Log.Error("DNM: should be off to StartAsHost");
                return;
            }

            SetState(DNMState.CreatingHost);

            // creates online server
            dualServer = serverDelegate(Mode.OnlineMode);

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
                Log.Debug("Stopped");
                
                SetState(DNMState.Off);

                var conn = dualServer.connections.GetConnection(DNM.LocalConnectionId);

                foreach(var p in conn.players)
                {
                    Log.Debug("Deleted one player");
                    OfflineDualPlayer op = (OfflineDualPlayer)p.actualPlayer;
                    Destroy(op.gameObject);
                }
                
                // TODO cleanup

                dualServer = null;
                dualClient = null;
                //clients = null;
                serverToLocalClientConnection = null;
            }
            else if(state == DNMState.Host)
            {
                // TODO destroy and clear things
                // TODO should clear clients ???????????????

                StopHost();

                dualServer = null;
                dualClient = null;
                //clients = null;
                serverToLocalClientConnection = null;
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

            // TODO

            acceptedConnections = new Dictionary<NetworkConnection, bool>();
            dualServer = null;
            dualClient = null;
            serverToLocalClientConnection = null;
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

            GetClient(DNM.LocalConnectionId).AddPlayer(player);
        }
        // TODO !!!
        public List<IDualPlayer> OfflinePlayers()
        {
            return GetClient(DNM.LocalConnectionId).players;
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

        /*
        public void ChangeRole(IDualPlayer player)
        {

            Log.Warn("ChangeRole not implemented");
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
        
            */
        // Server callbacks

        public override void OnServerConnect(NetworkConnection conn)
        {
            Log.Debug("### OnServerConnect({0}, {1})", state.ToString(), conn.connectionId);

            CheckState(new DNMState[] { DNMState.CreatingHost, DNMState.Host });

            int id = conn.connectionId;
            if(dualServer.connections.HasConnection(id))
            {
                Log.Error("Client already registered");
                return;
            }

            if(state == DNMState.CreatingHost)
            {
                if(id == 0)
                {
                    // this is just the local connection when starting as host

                    serverToLocalClientConnection = new ConnectionToClient(conn);

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

                    bool accepted = !sceneToggle.isOn; // TODO ask acceptance to dualServer

                    //SendStatusToRemoteClient(accepted, conn);

                    if(accepted)
                    {
                        acceptedConnections.Add(conn, true);
                        conn.Send(MsgType.ConnectionAccepted, new EmptyMessage());
                        //var connection = new ConnectionToClient(conn);
                        //dualServer.AddClient(id, connection);
                    }
                    else
                    {
                        conn.Disconnect();
                    }
                }
            }
            else
            {
                Log.Error("OnServerConnect: invalid state {0}", state);
                return;
            }
        }
        
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Log.Debug("### OnServerDisconnect({0})", conn.connectionId);

            // TODO avoid warnings for rejected clients

            // TODO pasar a DualServer en vez de violar encapsulamiento
            
            CheckState(DNMState.Host);

            NetworkServer.DestroyPlayersForConnection(conn);

            var id = conn.connectionId;

            if(dualServer.connections.HasConnection(id))
            {
                dualServer.RemoveClient(id);
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

            StringMessage msg = messageReader.ReadMessage<StringMessage>();


            var player = GameObject.Instantiate(onlinePlayerModel) as OnlineDualPlayer;
            //player.Init(conn.connectionId, playerControllerId);

            // spawned to get player.netId set
            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);
            var netId = player.netId.Value;
            if(netId == 0)
            {
                Log.Error("netId not set");
                return;
            }

            // initted to get updated in server
            var connId = conn.connectionId;
            player.Init(connId, playerControllerId);

            // added to server to get message stack initialization for remote clients
            var messageStack = dualServer.AddPlayer(player);


            NetworkServer.SendToAll(MsgType.NewPlayer, new MessageStackMessage(messageStack));


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
            int id = conn.connectionId;
            Log.Debug("### OnClientConnect({0}, {1}) hosted:{2}", state, id, NetworkServer.active ? "SI" : "no");

            // from UNET code
            // TODO !!!
            if(!clientLoadedScene)
            {
                ClientScene.Ready(conn);
            }

            // TODO check if id checking is correct

            if(state == DNMState.CreatingHost && id == DNM.LocalConnectionId/* && NetworkServer.active*/)
            {
                // we can consider here that a host has successfully started

                // creates online client
                dualClient = hostedClientDelegate(Mode.OnlineMode, dualServer);
                dualServer.AddLocalClient(dualClient, serverToLocalClientConnection);

                // this is just the local client connecting in Host mode
                SetState(DNMState.Host);
                RegisterServerHandlers();
                RegisterClientHandlers(conn);

                AddInitialPlayer(conn);
            }
            else if(state == DNMState.StartingAsClient && id != DNM.LocalConnectionId)
            {
                RegisterClientHandlers(conn);

                SetState(DNMState.WaitingAcceptanceAsClient);
                // Waiting for ConnectionAccepted message

            }
            else
            {
                Log.Warn("Unexpected call of OnClientConnect");
            }
        }

        // TODO move
        public void AddPlayerCommand(short playerControllerId)
        {
            var extraMessage = new StringMessage("Carlitos2"); // TODO ask player request data to client to send as extra message
            ClientScene.AddPlayer(null, playerControllerId, extraMessage);
        }

        public override void OnClientDisconnect(NetworkConnection conn) {
            // Log.Debug("### OnClientDisconnect({0})", conn);

            if(state == DNMState.WaitingAcceptanceAsClient)
            {
                Log.Debug("I was rejected :(");
            }
            else
            {
                Log.Warn("Unexpected call of OnClientDisconnect");
            }

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

        void AddInitialPlayer(NetworkConnection connectionToServer)
        {
            //ClientScene.Ready(connectionToServer); // TODO set ready here?
            AddPlayerCommand(0);
            if(sceneToggle.isOn) // TODO remove this!!!
            {
                AddPlayerCommand(1);
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

            NetworkServer.RegisterHandler(MsgType.InitialStateRequest, OnInitialStateRequestMessage);
            
            // Messaging
            NetworkServer.RegisterHandler(MsgType.GameClientToServer, OnGameClientToServerMessage);
        }

        void RegisterClientHandlers(NetworkConnection conn)
        {
            // Connecting
            conn.RegisterHandler(MsgType.ConnectionAccepted, OnConnectionAcceptedMessage);
            conn.RegisterHandler(MsgType.InitialState, OnInitialStateMessage);
            
            // Messaging
            conn.RegisterHandler(MsgType.GameServerToClient, OnGameServerToClientMessage);

            // Players
            conn.RegisterHandler(MsgType.NewPlayer, OnNewPlayerMessage);
        }
        
        // server handlers

        void OnInitialStateRequestMessage(NetworkMessage messageReader)
        {
            CheckState(DNMState.Host);

            var msg = messageReader.ReadMessage<StringMessage>();
            var username = msg.value;

            Log.Debug("'{0}' esta entrando al juego", username);

            var conn = messageReader.conn;

            if(!acceptedConnections.ContainsKey(conn))
            {
                Log.Error("Connection was not accepted");
                conn.Disconnect();
                return;
            }
            acceptedConnections.Remove(conn);

            dualServer.AddRemoteClient(new ConnectionToClient(messageReader.conn));

            SendStatusToRemoteClient(conn);
        }
        void SendStatusToRemoteClient(NetworkConnection conn)
        {
            var initialMessages = new List<MessageBase>();
            dualServer.WriteRemoteClientData(initialMessages);

            conn.Send(MsgType.InitialState, new MessageStackMessage(initialMessages));
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

        // client handlers

        // only in non-hosted clients
        void OnConnectionAcceptedMessage(NetworkMessage messageReader)
        {
            if(state != DNMState.WaitingAcceptanceAsClient)
            {
                Log.Warn("Unexpected ConnectionAccepted message");
                return;
            }

            Log.Debug("I was accepted :)");

            // creates non-hosted client
            dualClient = remoteClientDelegate();

            var username = "Carlitos"; // TODO

            client.Send(MsgType.InitialStateRequest, new StringMessage(username)); // TODO

            SetState(DNMState.WaitingInitialStateAsClient);
        }

        // only in non-hosted clients
        void OnInitialStateMessage(NetworkMessage messageReader)
        {
            CheckState(DNMState.WaitingInitialStateAsClient);

            SetState(DNMState.Client);

            var msg = messageReader.ReadMessage<MessageStackMessage>();

            dualClient.InitializeState(msg);

            AddInitialPlayer(messageReader.conn);
        }

        /*
        void OnClientInitialStatusMessage(NetworkMessage messageReader)
        {


            if(!msg.accepted)
            {
                Log.Warn("I was rejected :(");
                return;
            }

            RegisterClientHandlers(messageReader.conn);

            // creates non-hosted client
            dualClient = remoteClientDelegate(msg);

            SetState(DNMState.Client);
            //CreatePlayer(localClient.connection); // create remote initial player

            AddInitialPlayer(messageReader.conn);

            /*
            InstantiateClientAsync(msg, () =>
            {
                SetState(DNMState.Client);
                CreatePlayer(localClient.connection); // create remote initial player
            });
            * /
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

        void OnNewPlayerMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<MessageStackMessage>();

            dualClient.OnNewPlayerMessage(msg);
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
            foreach(var c in dualServer.connections.AllConnections().Values) // TODO TODO TODO TODO TODO 
            {
                int id = c.connectionId;

                if(id != who && c.connectionToClient.networkConnection.isReady) // TODO necessary right?
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
