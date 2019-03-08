using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace SyncGame
{
    public class SyncPlayerDisplay : MonoBehaviour, IDualPlayerListener, IGamePlayerListener, ITurnBasedPlayerListener
    {
        [Header("Colors")]
        public Color localColor = Color.black;
        public Color remoteColor = Color.black;

        public Color localPlayingColor = Color.green;
        public Color remotePlayingColor = Color.green;

        [Header("Hooks")]

        public Graphic colorDisplay;
        public InputField nameInput;
        public Text roleDisplay;
        public Button roleButton;
        public Button removeButton;

        public Toggle readyToggle;

        //// Dual basic data

        Mode mode;
        bool isHosted = false;
        
        SyncPlayer _syncPlayer;
        SyncPlayer syncPlayer
        {
            get
            {
                if(_syncPlayer == null)
                {
                    _syncPlayer = GetComponent<SyncPlayer>();
                    if(_syncPlayer == null)
                    {
                        Log.Error("Component TBPlayer not found!");
                    }
                }
                return _syncPlayer;
            }
        }

        void Awake()
        {
            syncPlayer.AddDualListener(this);
            syncPlayer.AddGameListener(this);
            syncPlayer.AddTBListener(this);
        }
        
        ////// IDualPlayerListener

        public void InitDualPlayer(Mode mode, bool isHosted = true, bool isLocal = true)
        {
            this.mode = mode;
            this.isHosted = isHosted;
        }
        
        ////// IGamePlayerListener

        public void InitGamePlayer(GamePlayerState state, int role, bool isReady, string username)
        {
            UpdateViews();
            UpdateInputs();
        }

        public void OnRoleChanged(int newRole)
        {
            UpdateRoleView();
        }

        public void OnReadyChanged(bool isReady)
        {
            UpdateReadyView();
        }

        public void OnNameChanged(string newName)
        {
            UpdateNameView();
        }

        public void OnNameRejected()
        {
            UpdateNameView();
            Log.Debug("Restoring name");
        }

        public void OnGameStarted()
        {
            UpdateInputs();
        }

        ////// ITurnBasedPlayerListener

        public void SetPlaying(bool isPlaying)
        {
            SetColor(GetColor(isPlaying));
        }

        //////////////////////////////////////////

        void UpdateViews()
        {
            UpdateNameView();
            UpdateRoleView();
            UpdateReadyView();
            UpdateColor();
        }

        void UpdateNameView()
        {
            nameInput.text = syncPlayer.username;
        }
        void UpdateRoleView()
        {
            roleDisplay.text = GetRoleText(syncPlayer.role);
        }
        void UpdateReadyView()
        {
            readyToggle.isOn = syncPlayer.isReady;
        }

        void UpdateColor()
        {
            SetColor(GetColor(false));
        }

        void UpdateInputs()
        {
            roleButton.interactable = isHosted;
            nameInput.interactable = syncPlayer.IsLocal();
            removeButton.interactable = syncPlayer.IsLocal();
        }

        //////////////////////////////////////////

        void SetColor(Color newColor)
        {
            colorDisplay.color = newColor;
        }

        //////////////////////////////////////////

        Color GetColor(bool isPlaying)
        {
            if(syncPlayer.IsLocal())
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

    } // class SyncPlayerDisplay

} // namespace SyncGame
