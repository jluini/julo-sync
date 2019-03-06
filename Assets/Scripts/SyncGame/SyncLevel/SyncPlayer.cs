using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace SyncGame
{
    //[RequireComponent(typeof(DualPlayer), typeof(GamePlayer), typeof(TBPlayer))]
    public class SyncPlayer : TBPlayer
    {
        public SyncPlayerDisplay display;

        /*
        [Header("Colors")]
        public Color localColor = Color.black;
        public Color remoteColor = Color.black;

        public Color localPlayingColor = Color.green;
        public Color remotePlayingColor = Color.green;

        [Header("Hooks")]

        // public Text nameDisplay;

        public Graphic colorDisplay;
        public InputField nameInput;
        public Text roleDisplay;
        public Button roleButton;

        public Toggle readyToggle;
        */
        /*
        void Awake()
        {
            if(display == null)
            {
                Log.Warn("No display");
                return;
            }

            this.AddDualListener(display);
            this.AddGameListener(display);
            this.AddTBListener(display);
        }
        */

    } // class SyncPlayer

} // namespace SyncGame
