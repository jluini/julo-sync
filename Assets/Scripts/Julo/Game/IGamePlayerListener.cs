using Julo.Users;

namespace Julo.Game
{
    public interface IGamePlayerListener
    {
        void InitGamePlayer(/*GameState gameState, */int role, bool isReady, string name);

        void OnRoleChanged(int newRole);
        void OnReadyChanged(bool isReady);
        void OnNameChanged(string newName);

        void OnGameStarted();

    } // interface IGamePlayerListener

} // namespace Julo.Game
