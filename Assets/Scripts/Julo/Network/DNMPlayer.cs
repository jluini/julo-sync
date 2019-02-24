using Julo.Users;

namespace Julo.Network
{

    public interface DNMPlayer : Player
    {
        void SetUser(UserProfile user);
        void SetRole(int newRole);

        bool IsReady();
        bool IsSpectator();

        void AddListener(DNMPlayerListener listener);
    }

} // namespace Julo.Network