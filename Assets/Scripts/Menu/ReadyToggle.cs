using UnityEngine;
using UnityEngine.UI;

using Julo.Network;

namespace Menu
{
    public class ReadyToggle : MonoBehaviour, ModeDisplay
    {
        public Toggle toggle;

        Mode mode = Mode.OfflineMode;

        public void OnValueChanged(bool newValue)
        {
            if(mode == Mode.OnlineMode)
            {
                DualNetworkManager.instance.ClientSetReadyCommand(newValue);
            }
        }

        public void SetMode(Mode mode, bool isHost = true)
        {
            if(mode == Mode.OfflineMode)
            {
                this.mode = Mode.OfflineMode;
                toggle.isOn = true;
                toggle.interactable = false;
            }
            else
            {
                // ensure it's temporarily offline to set isOn without triggering ClientSetReadyCommand
                this.mode = Mode.OfflineMode;

                toggle.isOn = false;
                toggle.interactable = true;

                this.mode = Mode.OnlineMode;
            }
        }

    } // class ReadyToggle

} // namespace Menu