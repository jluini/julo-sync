using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace Turtle
{
    [RequireComponent(typeof(IDualPlayer), typeof(GamePlayer), typeof(TBPlayer))]
    public class TurtlePlayerDisplay : MonoBehaviour, IDualPlayerListener, IGamePlayerListener, ITurnBasedPlayerListener
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

        ////

        Mode mode;
        bool isHosted = false;
        bool isLocal = false;

        void Awake()
        {
            var dualPlayer = GetComponent<IDualPlayer>();
            if(dualPlayer == null)
            {
                Log.Warn("Component IDualPlayer not found!");
                return;
            }

            dualPlayer.AddListener(this);

            var gamePlayer = GetComponent<GamePlayer>();
            if(gamePlayer == null)
            {
                Log.Warn("Component GamePlayer not found!");
                return;
            }

            gamePlayer.AddListener(this);

            var tbPlayer = GetComponent<TBPlayer>();
            if (tbPlayer == null)
            {
                Log.Warn("Component TBPlayer not found!");
                return;
            }
            
            tbPlayer.AddListener(this);
        }
        
        ////// IDualPlayerListener

        public void InitDualPlayer(Mode mode, bool isHosted = true, bool isLocal = true)
        {
            this.mode = mode;
            this.isHosted = isHosted;
            this.isLocal = isLocal;
        }
        
        ////// IGamePlayerListener

        public void InitGamePlayer(int role, bool isReady, string name)
        {
            nameDisplay.text = name;

            SetColor(GetColor(false)); // TODO ??

            roleDisplay.text = GetRoleText(role);
            roleButton.interactable = isHosted;

            // TODO do this here?
            readyToggle.isOn = isReady;
        }

        public void OnRoleChanged(int newRole)
        {
            roleDisplay.text = GetRoleText(newRole);
        }

        public void OnReadyChanged(bool isReady)
        {
            readyToggle.isOn = isReady;
        }

        public void OnNameChanged(string newName)
        {
            nameDisplay.text = newName;
        }

        ////// ITurnBasedPlayerListener

        public void SetPlaying(bool isPlaying)
        {
            // TODO implement
            SetColor(GetColor(isPlaying));
        }

        //////////////////////////////////////////
        
        void SetColor(Color newColor)
        {
            nameDisplay.color = newColor;
        }

        //////////////////////////////////////////

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

        string GetRoleText(int role)
        {
            return role == DNM.SpecRole ? "spec" : role.ToString();
        }

        //////////////////////////////////////////

    } // class TurtlePlayerDisplay

} // namespace Turtle
