using UnityEngine;

using Julo.Logging;
using Julo.Users;
using Julo.Panels;
using Julo.Network;
using Julo.Game;

using SyncGame.Menu;

namespace SyncGame
{
    public class GameManager : MonoBehaviour, IDualListener
    {
        [Header("Main")]
        public UserManager userManager;
        public DualNetworkManager dnm;

        [Header("Units")]
        public Unit unitModel;

        [Header("Panels")]
        public PanelManager panels;
        public Panel mainMenuPanel;
        public Panel connectingPanel;
        public LobbyPanel lobbyPanel;
        public Panel onlinePanel;

        GameServer gameServer;
        GameClient gameClient;

        DualServer CreateServer(Mode mode)
        {
            gameServer = new SyncServer(mode, unitModel);
            return gameServer;
        }

        DualClient CreateHostedClient(Mode mode, DualServer server)
        {
            gameClient = new SyncClient(mode, OnGameStarted, server);
            return gameClient;
        }

        DualClient CreateRemoteClient()
        {
            gameClient = new SyncClient(OnGameStarted, unitModel);
            return gameClient;
        }

        void Start()
        {
            userManager.Init();
            dnm.AddListener(this);

            //dnm.Init(userManager);



            dnm.Init(userManager, CreateServer, CreateHostedClient, CreateRemoteClient);
        }

        // Button handlers

        public void OnClickStartOffline()
        {
            dnm.StartOffline();
        }

        public void OnClickStartOnline()
        {
            // TODO
        }

        public void OnClickLanHost()
        {
            dnm.StartAsHost();
        }

        public void OnClickLanJoin()
        {
            dnm.StartAsClient();
        }

        public void OnClickStartGame()
        {
            //dnm.TryToStartGame();
            gameServer.TryToStartGame();
        }

        public void OnClickBack()
        {
            dnm.Stop();
        }

        public void OnClickCancelConnect()
        {
            if(dnm.GetState() != DNMState.StartingAsClient)
            {
                Log.Warn("GameManager: unexpected call of OnClickCancelConnect");
                return;
            }

            dnm.StopClient();
        }

        // DNMListener methods

        public void OnStateChanged(DNMState state)
        {
            if(state == DNMState.Off)
            {
                panels.OpenPanel(mainMenuPanel);
            }
            else if(state == DNMState.Offline)
            {
                lobbyPanel.SetMode(Mode.OfflineMode);
                panels.OpenPanel(lobbyPanel);
            }
            else if(state == DNMState.Host)
            {
                lobbyPanel.SetMode(Mode.OnlineMode, true);
                panels.OpenPanel(lobbyPanel);
            }
            else if(state == DNMState.CreatingHost)
            {
                // do nothing
            }
            else if(state == DNMState.StartingAsClient)
            {
                panels.OpenPanel(connectingPanel);
            }
            else if(state == DNMState.WaitingAcceptanceAsClient)
            {

            }
            else if(state == DNMState.WaitingInitialStateAsClient)
            {

            }
            else if(state == DNMState.Client)
            {
                lobbyPanel.SetMode(Mode.OnlineMode, false);
                panels.OpenPanel(lobbyPanel);
            }
            else
            {
                Log.Warn("Unknown new state: {0}", state);
            }
        }


        void OnGameStarted()
        {
            lobbyPanel.SetPlaying(true);
        }

    }

} // namespace SyncGame
