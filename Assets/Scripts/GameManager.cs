﻿using UnityEngine;
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
    public Panel connectingPanel;
    public LobbyPanel lobbyPanel;
    public Panel onlinePanel;

    void Start()
    {
        userManager.Init();
        dnm.AddListener(this);
        dnm.Init(userManager);
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
        dnm.TryToStartGame();
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


    public void OnClientGameStarted()
    {
        lobbyPanel.SetPlaying(true);
    }
    
}

