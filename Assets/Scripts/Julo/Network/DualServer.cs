using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    
    public class DualServer
    {
        public Dictionary<int, ConnectionToClient> connections;
        public static int LocalConnectionId = 0;

        public DualClient localClient = null;

        protected Mode mode;
        protected bool serverOnly;

        public DualServer(Mode mode, CreateHostedClientDelegate clientDelegate = null)
        {
            this.mode = mode;
            this.serverOnly = clientDelegate == null;

            this.connections = new Dictionary<int, ConnectionToClient>();

            if(clientDelegate != null)
            {
                // creates hosted client
                localClient = clientDelegate(mode, this);

                if(mode == Mode.OfflineMode)
                {
                    var singleConnection = new ConnectionToClient(null);
                    connections.Add(LocalConnectionId, singleConnection);
                    
                    // TODO add offline player !!!!!!!!!!
                }
            }
        }

        // a client tries to connect to this server
        public void OnConnect(NetworkConnection conn)
        {
            int id = conn.connectionId;
            if(connections.ContainsKey(id))
            {
                Log.Error("Client already registered");
                return;
            }

            bool accepted = false;
            bool connectionIsLocal = id == LocalConnectionId;

            if(connectionIsLocal)
            {
                if(serverOnly || connections.Count > 0)
                {
                    Log.Error("Unexpected local connection");
                    return;
                }
                // this is just the local connection when starting as host
                accepted = true;
            }
            else
            {
                if(!serverOnly && connections.Count == 0)
                {
                    Log.Warn("Remote client connecting before local one in host");
                }

                accepted = AcceptsRemoteClient();

                // TODO send status even if rejected?
                SendStatusToRemoteClient(accepted, conn);
            }

            if(accepted)
            {
                var ctc = new ConnectionToClient(conn);
                connections.Add(id, ctc);
            }
            else
            {
                conn.Disconnect();
            }
        }

        ///////////////////////
        
        public void TryToStartGame()
        {
            Log.Warn("TryToStartGame not implemented");
        }
        
        ///////////////////////

        protected virtual bool AcceptsRemoteClient()
        {
            // TODO accept criteria
            return true;
        }
        
        void SendStatusToRemoteClient(bool accepted, NetworkConnection conn)
        {
            var initialMessages = new List<MessageBase>();
            if(accepted)
            {
                WriteRemoteClientData(initialMessages);
            }
            Log.Debug("Sending InitialStatus message: {0}, {1}", accepted, initialMessages.Count);
            conn.Send(MsgType.InitialStatus, new StartRemoteClientMessage(accepted, initialMessages));
        }

        ///////////////////////
        
        // only online mode
        public virtual void WriteRemoteClientData(List<MessageBase> messages)
        {
            messages.Add(new UnityEngine.Networking.NetworkSystem.StringMessage("La vida loca"));
        }
        
        ///////////////////////

        // sending messages to clients
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...

        protected void SendTo(int destinationId, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(destinationId != LocalConnectionId)
                {
                    Log.Warn("Invalid connectionId != {0} in offline mode: {1}", LocalConnectionId, destinationId);
                    return;
                }

                localClient.SendMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendTo(destinationId, msgType, msg);
            }
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                localClient.SendMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
            }
        }

        protected void SendToAllBut(int connectionId, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(connectionId != 0)
                {
                    Log.Warn("Unexpected 'but' id {0} in offline mode", connectionId);
                }
                // sending to nobody
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAllBut(connectionId, msgType, msg);
            }
        }

        // message handling

        public void SendMessage(WrappedMessage message, int from)
        {
            OnMessage(message, from);
        }


        protected virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        /*
        protected virtual void OnStartServer() { }

        // only online mode
        public virtual void WriteInitialData(List<MessageBase> messages)
        {
            messages.Add(new UnityEngine.Networking.NetworkSystem.IntegerMessage(numRoles));
        }

        public abstract void StartGame();

        ///////// Messaging
        

        protected void SendTo(int who, short msgType, MessageBase msg)
        {
            DualNetworkManager.instance.GameServerSendTo(who, msgType, msg);
        }

        protected void SendToAll(short msgType, MessageBase msg)
        {
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...
            if(mode == Mode.OfflineMode)
            {
                GameClient.instance.OnMessage(new WrappedMessage(msgType, msg));
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAll(msgType, msg);
            }
        }

        protected void SendToAllBut(int who, short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                if(who != 0)
                {
                    Log.Warn("Unexpected 'but' id {0} in offline mode", who);
                }
            }
            else
            {
                DualNetworkManager.instance.GameServerSendToAllBut(who, msgType, msg);
            }
        }

        // only online mode
        public virtual void OnMessage(WrappedMessage message, int from)
        {
            throw new System.Exception("Unhandled message");
        }

        ///////// Utils
        
        protected List<Player> GetPlayersForRole(int role)
        {
            return playersPerRole[role - 1];
        }
        */
    } // class DNMServer

} // namespace Julo.Network