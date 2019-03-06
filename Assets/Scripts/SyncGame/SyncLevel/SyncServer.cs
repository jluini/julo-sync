using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace SyncGame
{
    public class SyncServer : TurnBasedServer
    {
        public static new SyncServer instance;

        Unit unitModel;

        SyncMatch match;

        public SyncServer(Mode mode, Unit unitModel) : base(mode)
        {
            instance = this;

            this.unitModel = unitModel;
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

        protected override void OnPrepareToStart(List<GamePlayer>[] playersPerRole, List<MessageBase> messageStack)
        {
            base.OnPrepareToStart(playersPerRole, messageStack);

            match = new SyncMatch();

            match.CreateFromSpawnPoints(
                numRoles,
                unitModel,
                Object.FindObjectsOfType<SpawnPoint>()
            );

            messageStack.Add(match.GetState());
        }

        protected override void OnStartTurn(int role)
        {
            SendToAll(MsgType.ServerUpdate, match.GetState());
        }

        public SyncMatch GetMatch()
        {
            return match;
        }

        protected override bool RoleIsAlive(int numRole)
        {
            return match.GetUnitsForRole(numRole).FindAll(t => !t.dead).Count > 0;
        }


        ////////// Messaging //////////

        protected override void OnMessage(WrappedMessage message, int from)
        {
            switch(message.messageType)
            {
                case MsgType.ClientUpdate:
                    // TODO check he is playing?

                    var msg = message.ReadInternalMessage<SyncGameState>();

                    match.UpdateState(msg);
                    
                    SendToAllBut(from, MsgType.ServerUpdate, msg); // TODO SendToAllRemoteBut ?

                    break;
                default:
                    base.OnMessage(message, from);
                    break;
            }
        }

    } // class SyncServer

} // namespace SyncGame

