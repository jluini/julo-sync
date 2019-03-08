using Julo.Users;

namespace Julo.Game
{
    public interface IGamePlayerListener
    {
        void InitGamePlayer(GamePlayerState playerState, int role, bool isReady, string name);

        void OnRoleChanged(int newRole);
        void OnReadyChanged(bool isReady);
        void OnNameChanged(string newName);

        void OnNameRejected();

        void OnGameStarted();

    } // interface IGamePlayerListener

} // namespace Julo.Game
