using Julo.Users;

namespace Julo.Network
{
    public interface DNMPlayerListener
    {

        void Init(string username, int role, DualNetworkManager.GameState gameState, Mode mode, bool isLocal = true);

        void OnReadyChanged(bool isReady);

        void OnRoleChanged(int newRole);

    }
}
