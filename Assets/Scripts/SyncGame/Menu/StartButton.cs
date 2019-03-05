using UnityEngine;
using UnityEngine.UI;

using Julo.Network;

namespace SyncGame.Menu
{
    public class StartButton : MonoBehaviour, ModeDisplay
    {
        public Button button;

        public void SetMode(Mode mode, bool isHost = true)
        {
            button.interactable = isHost;
        }

    } // class StartButton

} // namespace SyncGame.Menu