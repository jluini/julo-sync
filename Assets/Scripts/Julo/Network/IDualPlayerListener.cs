using Julo.Users;

namespace Julo.Network
{
    public interface IDualPlayerListener
    {

        void Init(/*string username, int role, DualNetworkManager.GameState gameState, */Mode mode, bool isHosted = true, bool isLocal = true);

        void OnReadyChanged(bool isReady);

        void OnRoleChanged(int newRole);

    }
}
