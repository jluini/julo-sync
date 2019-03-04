using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace TurtleGame
{
    public class TurtleServer : TurnBasedServer
    {
        public static new TurtleServer instance;

        Turtle offlineTurtleModel;
        Turtle onlineTurtleModel;

        TurtleMatch match;

        public TurtleServer(Mode mode, Turtle offlineTurtleModel, Turtle onlineTurtleModel) : base(mode)
        {
            instance = this;

            this.offlineTurtleModel = offlineTurtleModel;
            this.onlineTurtleModel = onlineTurtleModel;
        }

        // only online mode
        public override void WriteRemoteClientData(List<MessageBase> messages)
        {
            base.WriteRemoteClientData(messages);

            if(gameState == GameState.Playing || gameState == GameState.GameOver)
            {
                messages.Add(match.GetState());
            }

        }

        ////////// Player //////////

        public override void OnPlayerAdded(IDualPlayer player)
        {
            base.OnPlayerAdded(player);
        }

        public override void WritePlayer(IDualPlayer player, List<MessageBase> messageStack)
        {
            base.WritePlayer(player, messageStack);
        }

        protected override void OnPrepareToStart(List<MessageBase> messageStack)
        {
            base.OnPrepareToStart(messageStack);

            match = new TurtleMatch();

            match.CreateFromSpawnPoints(
                numRoles,
                mode == Mode.OfflineMode ? offlineTurtleModel : onlineTurtleModel,
                Object.FindObjectsOfType<SpawnPoint>()
            );

            messageStack.Add(match.GetState());
        }

        protected override void OnStartTurn(int role)
        {
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


        ////////// Messaging //////////

        protected override void OnMessage(WrappedMessage message, int from)
        {
            switch(message.messageType)
            {
                case MsgType.ClientUpdate:
                    // TODO check he is playing?

                    var msg = message.ReadInternalMessage<TurtleGameState>();

                    match.UpdateState(msg);
                    
                    SendToAllBut(from, MsgType.ServerUpdate, msg); // TODO SendToAllRemoteBut ?

                    break;
                default:
                    base.OnMessage(message, from);
                    break;
            }
        }

    } // class TurtleServer

} // namespace Turtle

