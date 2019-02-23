using Julo.Network;

namespace Julo.TurnBased
{
    public interface TBPlayer : Player
    {
        bool IsLocal();

        void AddListener(TBPlayerListener listener);
        void SetPlaying(bool isPlaying);
        
        //void TurnIsStartedRpc();
        //void TurnIsOverCommand();

        //void GameStateCommand();

    } // interface TBPlayer

} // namespace Julo.TurnBased

