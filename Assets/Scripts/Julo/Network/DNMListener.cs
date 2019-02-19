namespace Julo.Network
{
    public interface DNMListener
    {
        //void OnStateChanged(DualNetworkManager.DNMState newState);
        // void OnServerGameStateChanged(DualNetworkManager.GameState newState);
        // void OnClientGameStateChanged(DualNetworkManager.GameState newState);

        void OnClientStarted();

        void OnClientInitialStatus(string map, DualNetworkManager.GameState state);
        void OnClientGameWillStart(string map);
        void OnClientGameStarted();
    }
}

