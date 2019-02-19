using UnityEngine;

using Julo.Panels;
using Julo.Network;

namespace Menu
{

    public class LobbyPanel : Panel {

        public Transform gamePanel;

        Mode mode;
        bool isHost;

        public void SetMode(Mode mode, bool isHost = true)
        {
            this.mode = mode;
            this.isHost = isHost;
        }

        public void SetPlaying(bool isPlaying)
        {
            if(isPlaying)
            {
                gamePanel.gameObject.SetActive(false);
            }
        }

    } // class LobbyPanel

} // namespace Menu
