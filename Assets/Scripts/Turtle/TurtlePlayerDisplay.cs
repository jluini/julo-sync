using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;
using Julo.Network;
using Julo.TurnBased;

namespace Turtle
{
    [RequireComponent(typeof(DNMPlayer), typeof(TBPlayer))]
    public class TurtlePlayerDisplay : MonoBehaviour, DNMPlayerListener, TBPlayerListener
    {
        [Header("Colors")]
        public Color localColor = Color.black;
        public Color remoteColor = Color.black;

        public Color localPlayingColor = Color.green;
        public Color remotePlayingColor = Color.green;

        [Header("Hooks")]
        public Text nameDisplay;

        public Text roleDisplay;
        public Button roleButton;


        public Toggle readyToggle;

        bool isHosted = false;
        bool isLocal = false;

        void Awake()
        {
            DNMPlayer dnmPlayer = GetComponent<DNMPlayer>();
            if (dnmPlayer == null)
            {
                Log.Warn("Component DNMPlayer not found!");
                return;
            }

            dnmPlayer.AddListener(this);

            TBPlayer tbPlayer = GetComponent<TBPlayer>();
            if (tbPlayer == null)
            {
                Log.Warn("Component TBPlayer not found!");
                return;
            }
            
            tbPlayer.AddListener(this);
        }
        
        // TBPlayerListener
        //////////////////////////////////////////

        public void SetPlaying(bool isPlaying)
        {
            // TODO implement
            SetColor(GetColor(isPlaying));
        }

        // DNMPlayerListener
        //////////////////////////////////////////

        public void Init(string username, int role, DualNetworkManager.GameState gameState, Mode mode, bool isHosted = true, bool isLocal = true)
        {
            this.isHosted = isHosted;
            this.isLocal = isLocal;

            nameDisplay.text = username;

            SetColor(GetColor(false));

            roleDisplay.text = GetRoleText(role);
            roleButton.interactable = isHosted;

            // TODO do this here?
            readyToggle.isOn = mode == Mode.OfflineMode;
        }

        void SetColor(Color newColor)
        {
            nameDisplay.color = newColor;
        }

        Color GetColor(bool isPlaying)
        {
            if(isLocal)
            {
                return isPlaying ? localPlayingColor : localColor;
            }
            else
            {
                return isPlaying ? remotePlayingColor : remoteColor;
            }
        }

        public void OnReadyChanged(bool isReady)
        {
            readyToggle.isOn = isReady;
        }

        public void OnRoleChanged(int newRole)
        {
            roleDisplay.text = GetRoleText(newRole);
        }

        string GetRoleText(int role)
        {
            return role == DNM.SpecRole ? "spec" : role.ToString();
        }

        //////////////////////////////////////////

    } // class TurtlePlayerDisplay

} // namespace Turtle
