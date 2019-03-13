using UnityEngine.Networking;

using Julo.Network;

namespace Julo.TurnBased
{
    
    public class TurnBasedContext
    {

        public TurnBasedPlayer currentPlayer;

        // in server
        public TurnBasedContext()
        {
            currentPlayer = null;
        }

        // in remote client
        public TurnBasedContext(DualPlayerSnapshot snapshot)
        {
            currentPlayer = (TurnBasedPlayer)DualContext.instance.GetPlayer(snapshot);
        }

        public MessageBase GetState()
        {
            return new DualPlayerSnapshot(currentPlayer);
        }

    } // class TurnBasedContext

} // namespace Julo.TurnBased
