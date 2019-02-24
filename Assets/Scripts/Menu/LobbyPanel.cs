using UnityEngine;

using Julo.Panels;
using Julo.Network;

namespace Menu
{
    public class LobbyPanel : Panel, ModeDisplay {

        [Header("Hooks")]
        public Transform gamePanel;

        public ReadyToggle readyToggle;
        public StartButton startButton;

        Mode mode;
        bool isHost;

        public void SetMode(Mode mode, bool isHost = true)
        {
            this.mode = mode;
            this.isHost = isHost;

            readyToggle.SetMode(mode, isHost);
            startButton.SetMode(mode, isHost);
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
