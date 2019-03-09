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

        NetworkConnection serverToLocalClientConnection = null;

        ////////////////////////////////////////////////////////////

        List<IDualListener> listeners = new List<IDualListener>();

        ////////////////////////////////////////////////////////////

        DualContext dualContext
        {
            get
            {
                if(dualServer != null)
                {
                    return dualServer.dualContext;
                }
                if(dualClient != null)
                {
                    return dualClient.dualContext;
                }
                Log.Error("No server and no client");
                return null;
            }
        }

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

            serverToLocalClientConnection = null;
            dualServer.AddLocalClient(dualClient, null);

            dualServer.AddPlayer(0, 0); //AddOfflinePlayer(0);
            if(sceneToggle.isOn) // TODO remove this!!!
            {
                dualServer.AddPlayer(0, 1); // AddOfflinePlayer(1);
            }

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

                /*var conn = dualServer.dualContext.GetConnection(DNM.LocalConnectionId);

                foreach(var p in conn.players.Values)
                {
                    Destroy(p.gameObject);
                }
                */

                // TODO offline case
                Log.Debug("TODO implement!!!!");

                //dualServer.RemoveClient(DNM.LocalConnectionId);
                
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
                dualClient.StopClient();
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
            if(dualServer.dualContext.HasConnection(id))
            {
                Log.Error("Client already registered");
                return;
            }

            if(state == DNMState.CreatingHost)
            {
                if(id == 0)
                {
                    // this is just the local connection when starting as host

                    serverToLocalClientConnection = conn;
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

            var id = conn.connectionId;

            dualServer.OnClientDisconnected(id);

            /*
            foreach(var player in dualServer.dualContext.GetPlayers(id))
            {
                //dualServer.connections.RemovePlayer

                // TODO send to remote only
                NetworkServer.SendToAll(MsgType.RemovePlayer, new DualPlayerSnapshot(player));
            }
                
            dualServer.RemoveClient(id);

            if(conn.lastError != NetworkError.Ok)
            {
                Log.Error("ServerDisconnected due to error: " + conn.lastError);
                // if (LogFilter.logError) { Debug.LogError("ServerDisconnected due to error: " + conn.lastError); }
            }

            // Log.Debug("A client disconnected from the server: " + conn);
            */
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            NetworkServer.SetClientReady(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId/*, NetworkReader messageReader*/)
        {
            if(state != DNMState.Host)
            {
                Log.Warn("Unexpected call of OnServerAddPlayer");
                return;
            }

            // TODO custom addPlayer message

            var listOfMessages = dualServer.AddPlayer(conn.connectionId, playerControllerId);

            NetworkServer.SendToAll(MsgType.NewPlayer, listOfMessages);
        }
        /*
        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId) {
            Log.Error("OnServerAddPlayer should not be called without extra message");
        }*/

        public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player) {
            /* TODO
            if(player.gameObject != null)
                NetworkServer.Destroy(player.gameObject);
            */
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
            else if(state == DNMState.StartingAsClient || state == DNMState.WaitingAcceptanceAsClient)
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
                //Log.Warn("    with state {0}", state);
                Log.Warn("Unexpected call of OnStopClient: state {0}", state);
                SetState(DNMState.Off);
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

        public void AddPlayerCommand()
        {
            AddPlayerCommand(NextControllerId());
        }

        short NextControllerId()
        {
            short ret = 0;

            while(true)
            {
                if(!LocalPlayerWithControllerId(ret))
                {
                    return ret;
                }
                else
                {
                    ret++;
                }
            }
        }

        bool LocalPlayerWithControllerId(short controllerId)
        {
            var conn = dualContext.GetConnection(DNM.LocalConnectionId);
            if(conn == null)
            {
                Log.Error("Local connection not found");
                return false;
            }
             
            foreach(var playerControllerId in conn.players.Keys)
            {
                if(playerControllerId == controllerId)
                {
                    return true;
                }
            }
            return false;
        }

        public void AddPlayerCommand(short playerControllerId)
        {
            if(state == DNMState.Offline)
            {
                dualServer.AddPlayer(DNM.LocalConnectionId, NextControllerId());
            }
            else if(state == DNMState.Host || state == DNMState.Client)
            {
                // TODO extra message?
                ClientScene.AddPlayer(null, playerControllerId, null);
            }
            else
            {
                Log.Error("Unexpected call of AddPlayerCommand: {0}", state);
            }
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
            conn.RegisterHandler(MsgType.RemovePlayer, OnRemovePlayerMessage);
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

            dualServer.AddRemoteClient(messageReader.conn);

            SendStatusToRemoteClient(conn);
        }

        void SendStatusToRemoteClient(NetworkConnection conn)
        {
            var listOfMessages = new ListOfMessages();
            listOfMessages.Add(new IntegerMessage(conn.connectionId));
            dualServer.WriteRemoteClientData(listOfMessages);

            conn.Send(MsgType.InitialState, listOfMessages);
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
            //dualClient = remoteClientDelegate();

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

            var listOfMessages = messageReader.ReadMessage<ListOfMessages>();

            var connectionNumberMsg = listOfMessages.ReadMessage<IntegerMessage>();
            var connectionNumber = connectionNumberMsg.value;

            // creates non-hosted client
            dualClient = remoteClientDelegate();

            dualClient.InitializeState(connectionNumber, listOfMessages);

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

        // only client
        void OnNewPlayerMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<ListOfMessages>();

            dualClient.OnNewPlayerMessage(msg);
        }

        void OnRemovePlayerMessage(NetworkMessage messageReader)
        {
            var msg = messageReader.ReadMessage<DualPlayerSnapshot>();

            if(!NetworkServer.active)
            {
                // TODO
                //dualClient.RemovePlayer(msg.playerId);
            }
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
            
            foreach(var conn in NetworkServer.connections)
            {
                // TODO
                //foreach(var c in dualServer.dualContext.AllConnections()) // TODO encapsulamiento...
                //{
                //int id = c.connectionId;
                // var conn = c.networkConnection;
                int id = conn.connectionId;

                if(id != who && conn.isReady) // TODO necessary, right?
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
