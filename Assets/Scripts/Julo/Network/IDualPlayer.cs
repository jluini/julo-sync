using Julo.Users;

namespace Julo.Network
{
    // TODO unificate with IPlayer?

    public interface IDualPlayer : IPlayer
    {
        
        int ConnectionId();
        short ControllerId();

        void AddListener(IDualPlayerListener listener);
    }

} // namespace Julo.Network