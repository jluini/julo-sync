using Julo.Network;

namespace Julo.TurnBased
{
    public interface TBPlayer : Player
    {
        void AddListener(TBPlayerListener listener);
        void SetPlaying(bool isPlaying);
        void TurnIsStartedRpc();
        void TurnIsOverCommand();
        
    } // interface TBPlayer

} // namespace Julo.TurnBased

