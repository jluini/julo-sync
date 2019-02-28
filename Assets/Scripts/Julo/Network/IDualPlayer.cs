using Julo.Users;

namespace Julo.Network
{
    // TODO needed?

    public interface IDualPlayer : IPlayer
    {
        void AddListener(IDualPlayerListener listener);
        
        // all this is game-level

        //void SetUser(UserProfile user);
        //void SetRole(int newRole);

        //bool IsReady();
        //bool IsSpectator();
    }

} // namespace Julo.Network