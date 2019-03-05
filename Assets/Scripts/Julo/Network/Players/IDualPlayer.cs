using Julo.Users;

namespace Julo.Network
{
    // Unificates OfflineDualPlayer / OnlineDualPlayer

    public interface IDualPlayer : IPlayer
    {
        void AddListener(IDualPlayerListener listener);
    }

} // namespace Julo.Network