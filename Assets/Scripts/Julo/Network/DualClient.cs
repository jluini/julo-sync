using UnityEngine.Networking;

using Julo.Logging;

namespace Julo.Network
{
    public class DualClient
    {
        protected Mode mode;
        protected bool isHosted;

        protected DualServer server;

        // creates hosted client
        public DualClient(Mode mode, DualServer server)
        {
            this.mode = mode;
            this.server = server;
            isHosted = true;
        }

        // creates remote client
        public DualClient(StartRemoteClientMessage startMessage)
        {
            this.mode = Mode.OnlineMode;
            server = null;
            isHosted = false;

            var message = startMessage.ReadInitialMessage<UnityEngine.Networking.NetworkSystem.StringMessage>();
            Log.Debug("DualClient: recibi {0}", message.value);
        }

        // sending messages to server
            // TODO tratar de no recibirlo wrapped
            // TODO usar polimorfismo en vez de if...
        protected void SendToServer(short msgType, MessageBase msg)
        {
            if(mode == Mode.OfflineMode)
            {
                server.SendMessage(new WrappedMessage(msgType, msg), 0);
            }
            else
            {
                DualNetworkManager.instance.GameClientSendToServer(msgType, msg);
            }
        }

        // message handling

        // send a message to this client
        public void SendMessage(WrappedMessage message)
        {
            OnMessage(message);
        }

        protected virtual void OnMessage(WrappedMessage message)
        {
            throw new System.Exception("Unhandled message");
        }

    } // class DualClient

} // namespace Julo.Network