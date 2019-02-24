namespace Julo.Network
{
    public interface DNMListener
    {
        void OnStateChanged(DNMState newState);
        void OnClientGameStarted();
    }
}

