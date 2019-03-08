using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

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

        public SyncServer(Mode mode, DualPlayer playerModel, Unit unitModel) : base(mode, playerModel)
        {
            instance = this;

            this.unitModel = unitModel;
        }

        // only online mode
        public override void WriteRemoteClientData(ListOfMessages listOfMessages)
        {
            base.WriteRemoteClientData(listOfMessages);

            if(gameContext.gameState == GameState.Playing || gameContext.gameState == GameState.GameOver)
            {
                listOfMessages.Add(match.GetSnapshot());
            }
        }

        ////////// Player //////////

        protected override void OnPrepareToStart(List<GamePlayer>[] playersPerRole, ListOfMessages listOfMessages)
        {
            base.OnPrepareToStart(playersPerRole, listOfMessages);

            match = new SyncMatch();

            match.CreateFromSpawnPoints(
                gameContext.numRoles,
                unitModel,
                Object.FindObjectsOfType<SpawnPoint>()
            );

            listOfMessages.Add(match.GetSnapshot());
        }

        protected override void OnStartTurn(int role)
        {
            SendToAll(MsgType.ServerUpdate, match.GetSnapshot());
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

                    var msg = message.ReadInternalMessage<SyncMatchSnapshot>();

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

