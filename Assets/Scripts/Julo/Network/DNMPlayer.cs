using Julo.Users;

namespace Julo.Network
{

    public interface DNMPlayer : Player
    {
        bool IsLocal();

        void AddListener(DNMPlayerListener listener);

        void SetUser(UserProfile user);

        bool IsReady();

        bool IsSpectator();

    }

} // namespace Julo.Network