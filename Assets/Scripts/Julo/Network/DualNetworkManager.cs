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

        ////////////////////////////////////////////////////////////

        DualServer dualServer = null;
        DualClient dualClient = null;

        ////////////////////////////////////////////////////////////

        UserManager userManager = null;
        DNMState state = DNMState.NotInitialized;

        NetworkClient localClient = null;

        ////////////////////////////////////////////////////////////

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
            dualServer.AddPlayer(player);
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

                var conn = dualServer.connections.GetConnection(DNM.LocalConnectionId);

                foreach(var p in conn.players)
                {
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

        /////// SERVER

        // Server callbacks

        public override void OnServerConnect(NetworkConnection conn)
        {
            // if(conn.connectionId > 0) Log.Debug("### OnServerConnect({0}, {1})", state.ToString(), conn.connectionId);

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

                    if(accepted)
                    {
                        acceptedConnections.Add(conn, true);
                        conn.Send(MsgType.ConnectionAccepted, new EmptyMessage());
                    }
                    else
                    {
                        Log.Debug("Disconnecting");
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

            // TODO pasar a DualServer en vez de violar encapsulamiento?
            
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
            else if(state == DNMState.WaitingAcceptanceAsClient)
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
            // if(!NetworkServer.active) Log.Debug("### OnClientConnect({0}, {1}) hosted:{2}", state, id, NetworkServer.active ? "SI" : "no");

            if(state == DNMState.CreatingHost && id == DNM.LocalConnectionId)
            {
                // we can consider here that a host has successfully started

                // creates online client
                dualClient = hostedClientDelegate(Mode.OnlineMode, dualServer);
                dualServer.AddLocalClient(dualClient, serverToLocalClientConnection);

                // this is just the local client connecting in Host mode
                SetState(DNMState.Host);
                RegisterServerHandlers();
                RegisterClientHandlers(conn);

                GetReady(conn);
                AddInitialPlayer();
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

            if(state == DNMState.StartingAsClient || state == DNMState.WaitingAcceptanceAsClient)
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

        void GetReady(NetworkConnection conn)
        {
            // from UNET code
            // TODO !!!
            if(clientLoadedScene)
            {
                Log.Warn("Cuando pasa esto??");
            }

            ClientScene.Ready(conn);
        }

        void AddInitialPlayer()
        {
            AddPlayerCommand(0);
            if(sceneToggle.isOn) // TODO remove this!!!
            {
                AddPlayerCommand(1);
            }
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

            // creates non-hosted client
            dualClient = remoteClientDelegate();

            var username = "Carlitos"; // TODO

            GetReady(messageReader.conn);

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

            AddInitialPlayer();
        }

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
        
        //// IDualListener

        public void AddListener(IDualListener listener)
        {
            listeners.Add(listener);
        }

        // Server utils
        
        //// Messaging

        // server
        void SendToAll(short msgType, MessageBase message)
        {
            CheckState(DNMState.Host);

            NetworkServer.SendToAll(msgType, message);
        }

        // TODO no longer call this "game" level
        // TODO remove "game" word

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

        // TODO remove "game" word
        public void GameServerSendToAllBut(int who, short msgType, MessageBase gameMessage)
        {
            CheckState(DNMState.Host);
            
            foreach(var c in dualServer.connections.AllConnections().Values) // TODO encapsulamiento
            {
                int id = c.connectionId;

                if(id != who && c.connectionToClient.networkConnection.isReady) // TODO necessary, right?
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

    } // class DualNetworkManager

} // namespace Julo.Network
