using System.Collections.Generic;

using Julo.Logging;

namespace Julo.Network
{
    public class PlayerInfo : IDualPlayer
    {

        public IDualPlayer actualPlayer;
        public DualPlayerMessage playerScreenshot;

        // in server
        public PlayerInfo(IDualPlayer player)
        {
            this.actualPlayer = player;
            playerScreenshot = new DualPlayerMessage(player);
        }

        // in client; actualPlayer can be null if it wasn't resolved yet
        public PlayerInfo(IDualPlayer actualPlayer, DualPlayerMessage playerScreenshot)
        {
            this.actualPlayer = actualPlayer;
            this.playerScreenshot = playerScreenshot;
        }

        public uint PlayerId()
        {
            return playerScreenshot.PlayerId();
        }

        public bool IsLocal()
        {
            if(actualPlayer != null)
            {
                return actualPlayer.IsLocal();
            }
            Log.Error("Not resolved yet");
            return false;
        }

        public int ConnectionId()
        {
            return playerScreenshot.ConnectionId();
        }

        public short ControllerId()
        {
            return playerScreenshot.ControllerId();
        }

        public void AddListener(IDualPlayerListener listener)
        {
            throw new System.Exception();
        }

    }

} // namespace Julo.Network
