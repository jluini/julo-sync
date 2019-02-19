using Julo.Users;

namespace Julo.Network
{

    public interface DNMPlayer : Player
    {
        void AddListener(DNMPlayerListener listener);

        void SetUser(UserProfile user);

        bool IsReady();

        bool IsSpectator();
    }

} // namespace Julo.Network