using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;
using Julo.Network;
using Julo.Game;
using Julo.TurnBased;

namespace SyncGame
{
    [RequireComponent(typeof(DualPlayer), typeof(GamePlayer), typeof(TBPlayer))]
    public class SyncPlayerDisplay : MonoBehaviour, IDualPlayerListener, IGamePlayerListener, ITurnBasedPlayerListener
    {
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

        //// Dual

        Mode mode;
        bool isHosted = false;
        bool isLocal = false;

        //// Game

        GameState gameState = GameState.Unknown;
        int role;
        string name;
        bool isReady;

        DualPlayer _dualPlayer;
        DualPlayer dualPlayer
        {
            get
            {
                if(_dualPlayer == null)
                {
                    _dualPlayer = GetComponent<DualPlayer>();
                    if(_dualPlayer == null)
                    {
                        Log.Error("Component DualPlayer not found!");
                    }
                }
                return _dualPlayer;
            }
        }

        GamePlayer _gamePlayer;
        GamePlayer gamePlayer
        {
            get
            {
                if(_gamePlayer == null)
                {
                    _gamePlayer = GetComponent<GamePlayer>();
                    if(_gamePlayer == null)
                    {
                        Log.Error("Component GamePlayer not found!");
                    }
                }
                return _gamePlayer;
            }
        }

        TBPlayer _tbPlayer;
        TBPlayer tbPlayer
        {
            get
            {
                if(_tbPlayer == null)
                {
                    _tbPlayer = GetComponent<TBPlayer>();
                    if(_tbPlayer == null)
                    {
                        Log.Error("Component TBPlayer not found!");
                    }
                }
                return _tbPlayer;
            }
        }

        void Awake()
        {
            dualPlayer.AddListener(this);
            gamePlayer.AddListener(this);
            tbPlayer.AddListener(this);
        }
        
        ////// IDualPlayerListener

        public void InitDualPlayer(Mode mode, bool isHosted = true, bool isLocal = true)
        {
            // Log.Debug("InitDualPlayer {0} {1} {2}", mode, isHosted, isLocal);

            this.mode = mode;
            this.isHosted = isHosted;
            this.isLocal = isLocal;
        }
        
        ////// IGamePlayerListener

        public void InitGamePlayer(/*GameState gameState, */int role, bool isReady, string name)
        {
            // Log.Debug("InitGamePlayer {0} {1} {2}", role, isReady, name);

            //this.gameState = gameState;
            this.role = role;
            this.isReady = isReady;
            this.name = name;

            UpdateViews();
            UpdateInputs();
        }

        void UpdateViews()
        {
            UpdateNameView();
            UpdateRoleView();
            UpdateReadyView();
            UpdateColor();
        }

        void UpdateNameView()
        {
            nameInput.text = name;
        }
        void UpdateRoleView()
        {
            roleDisplay.text = GetRoleText(role);
        }
        void UpdateReadyView()
        {
            readyToggle.isOn = isReady;
        }

        void UpdateColor()
        {
            SetColor(GetColor(false));
        }

        void UpdateInputs()
        {
            /*
            GameState gameState = GameState.Unknown;

            var gameServer = GameServer.instance;

            if(gameServer != null)
            {
                gameState = gameServer.gameState;
            }
            else
            {
                gameState = GameClient.instance.gameState;
            }
            */
            // Log.Debug("Updating inputs: {0} {1}", isHosted/*, gameState*/);

            roleButton.interactable = isHosted/* && gameState == GameState.NoGame*/;
            nameInput.interactable = isLocal/* && gameState == GameState.NoGame*/;
        }

        public void OnRoleChanged(int newRole)
        {
            this.role = newRole;
            UpdateRoleView();
        }

        public void OnReadyChanged(bool isReady)
        {
            this.isReady = isReady;
            UpdateReadyView();
        }

        public void OnNameChanged(string newName)
        {
            this.name = newName;
            UpdateNameView();
        }

        public void OnGameStarted()
        {
            //gameState = GameState.Playing;
            UpdateInputs();
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
            colorDisplay.color = newColor;
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
        
        public void OnNameEntered(string newName)
        {
            var changed = GameClient.instance.ChangeName(gamePlayer, newName);

            if(!changed)
            {
                nameInput.text = name;
            }
        }

    } // class SyncPlayerDisplay

} // namespace SyncGame
