using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;
using Julo.Users;
using Julo.Panels;
using Menu;

public class GameManager : MonoBehaviour, DNMListener
{

    public UserManager userManager;

    public DualNetworkManager dnm;

    public PanelManager panels;
    public Panel mainMenuPanel;
    public LobbyPanel lobbyPanel;
    public Panel onlinePanel;

    void Start()
    {
        userManager.Init();

        dnm.Init(userManager);
        dnm.AddListener(this);

        panels.OpenPanel(mainMenuPanel);
    }

    public void StartOffline()
    {
        dnm.StartOffline();

        lobbyPanel.SetMode(Mode.OfflineMode);
        panels.OpenPanel(lobbyPanel);
    }

    public void StartOnline()
    {
        //panels.OpenPanel(onlinePanel);
    }

    public void StartLANHost()
    {
        if(dnm.StartAsHost())
        {
            //lobbyPanel.SetMode(Mode.OnlineMode, true);
            //panels.OpenPanel(lobbyPanel);
        }
    }

    public void StartLANJoin()
    {
        if(dnm.StartAsClient())
        {
            // lobbyPanel.SetMode(Mode.OnlineMode, false);
            // panels.OpenPanel(lobbyPanel);
        }
        else
        {
            Log.Warn("Can't join localhost game");
        }
    }

    // DNMListener methods

    public void OnStateChanged(DualNetworkManager.DNMState newState)
    {
        
    }
    /*
    public void OnServerGameStateChanged(DualNetworkManager.GameState newState)
    {
        // ...
    }

    public void OnClientGameStateChanged(DualNetworkManager.GameState newState)
    {
        Log.Debug("### {0} ###", newState);

        if(newState == DualNetworkManager.GameState.NoGame)
        {
            // TODO implement
            throw new System.NotImplementedException();
        }
        else if(newState == DualNetworkManager.GameState.Lobby)
        {
            panels.OpenPanel(lobbyPanel);
            lobbyPanel.SetPlaying(false);
        }
        else if(newState == DualNetworkManager.GameState.Playing)
        {
            panels.OpenPanel(lobbyPanel);
            lobbyPanel.SetPlaying(true);
        }
        else
        {
            Log.Debug("----");
        }
    }
    */
    public void OnClientStarted()
    {
        // lobbyPanel.SetMode(Mode.OnlineMode, NetworkServer.active);
        // panels.OpenPanel(lobbyPanel);
    }
    public void OnClientInitialStatus(string map, DualNetworkManager.GameState state)
    {
        // TODO use map
        if(state == DualNetworkManager.GameState.NoGame)
        {
            Log.Error("Unexpected state NoGame");
        }
        else if(state == DualNetworkManager.GameState.Lobby)
        {
            panels.OpenPanel(lobbyPanel);
            lobbyPanel.SetPlaying(false);
        }
        else if(state == DualNetworkManager.GameState.Playing)
        {
            panels.OpenPanel(lobbyPanel);
            lobbyPanel.SetPlaying(true);
        }
        // TODO check this case...
        else if(state == DualNetworkManager.GameState.WillStart)
        {
            panels.OpenPanel(lobbyPanel);
            lobbyPanel.SetPlaying(true);
        }
        else
        {
            Log.Debug("----");
        }
    }

    public void OnClientGameWillStart(string map)
    {
        
    }
    public void OnClientGameStarted()
    {
        lobbyPanel.SetPlaying(true);
    }
}

