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
        public GameContext(GameState gameState, int numRoles, string sceneName)
        {
            this.gameState = gameState;
            this.numRoles = numRoles;
            this.sceneName = sceneName;
        }

    } // class GameContext

} // namespace Julo.Game