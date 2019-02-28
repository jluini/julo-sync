using System.Collections;
using System.Collections.Generic;

using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;
using Julo.TurnBased;

namespace Turtle
{
    public class TurtleServer : TurnBasedServer
    {

        public TurtleServer(Mode mode, CreateHostedClientDelegate clientDelegate = null) : base(mode, clientDelegate)
        {
            //instance = this;
            Log.Debug("Creating TurtleServer");
        }

        // only online mode
        public override void WriteRemoteClientData(List<MessageBase> messages)
        {
            base.WriteRemoteClientData(messages);

            // TODO pass data to TurtleClient

            messages.Add(new UnityEngine.Networking.NetworkSystem.StringMessage("Estamos a nivel Turtle"));
        }

        protected override void OnMessage(WrappedMessage message, int from)
        {
            base.OnMessage(message, from);
        }


        /*
        public static new TurtleServer instance = null;

        public Turtle onlineTurtlePrefab;
        public Turtle offlineTurtlePrefab;

        TurtleMatch match;

        protected override void OnStartServer()
        {
            base.OnStartServer();

            instance = this;

            match = new TurtleMatch();
            match.CreateFromSpawnPoints(
                numRoles,
                mode == Mode.OfflineMode ? offlineTurtlePrefab : onlineTurtlePrefab,
                FindObjectsOfType<SpawnPoint>()
            );
        }

        // only online mode
        public override void WriteInitialData(List<MessageBase> messages)
        {
            base.WriteInitialData(messages);

            var initialState = match.GetState();
            messages.Add(initialState);
        }

        protected override void OnStartGame()
        {
            // noop
        }

        protected override void OnStartTurn(int role)
        {
            // TODO send  only to remote?
            SendToAll(MsgType.ServerUpdate, match.GetState());
        }

        public TurtleMatch GetMatch()
        {
            return match;
        }

        protected override bool RoleIsAlive(int numRole)
        {
            return match.GetTurtlesForRole(numRole).FindAll(t => !t.dead).Count > 0;
        }
        
        public override void OnMessage(WrappedMessage message, int from)
        {
            short msgType = message.messageType;

            if(msgType == MsgType.ClientUpdate)
            {
                // TODO check that is playing player?

                var msg = message.ReadExtraMessage<GameState>();

                match.UpdateState(msg);

                // TODO SendToAllRemoteBut ?
                SendToAllBut(from, MsgType.ServerUpdate, msg);
            }
            else
            {
                base.OnMessage(message, from);
            }
        }
        */
    } // class TurtleServer

} // namespace Turtle

