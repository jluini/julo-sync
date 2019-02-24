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
        public Color defaultColor = Color.gray;
        public Color playingColor = Color.green;

        [Header("Hooks")]
        public Text nameDisplay;
        public Text roleDisplay;
        public Toggle readyToggle;

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
            nameDisplay.color = isPlaying ? playingColor : defaultColor;
        }

        // DNMPlayerListener
        //////////////////////////////////////////

        public void Init(string username, int role, DualNetworkManager.GameState gameState, Mode mode, bool isLocal = true)
        {
            nameDisplay.text = username;
            roleDisplay.text = GetRoleText(role);
            readyToggle.isOn = false;

            this.isLocal = isLocal;
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
