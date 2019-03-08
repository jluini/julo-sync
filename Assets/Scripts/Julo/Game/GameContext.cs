using UnityEngine.Networking;

namespace Julo.Game
{
    public class GameContext
    {
        public GameState gameState;
        public int numRoles;
        public string sceneName;

        // in server
        public GameContext()
        {
            gameState = GameState.NoGame;
            numRoles = 2;
            sceneName = "beach";
        }
        
        // in remote client
        public GameContext(GameContextSnapshot snapshot)
        {
            gameState = snapshot.gameState;
            numRoles = snapshot.numRoles;
            sceneName = snapshot.sceneName;
        }

        public GameContextSnapshot GetSnapshot()
        {
            return new GameContextSnapshot(gameState, numRoles, sceneName);
        }

    } // class GameContext

} // namespace Julo.Game