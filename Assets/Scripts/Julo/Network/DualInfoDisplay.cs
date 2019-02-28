using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Julo.Logging;

namespace Julo.Network
{
    
    public class DualInfoDisplay : MonoBehaviour, InfoDisplay
    {
        public Text dnmStateDisplay;
        public Text gameStateDisplay;

        Dictionary<string, Text> displays = new Dictionary<string, Text>();

        void Start()
        {
            Info.AddInfoDisplay(this);

            displays.Add("DNMState", dnmStateDisplay);
            displays.Add("GameState", gameStateDisplay);
        }

        public void Set(string key, string value)
        {
            if(displays.ContainsKey(key))
            {
                displays[key].text = value;
            }
            else
            {
                Log.Warn("Cannot display that: {0}", key);
            }
        }

    } // class DualInfoDisplay

} // namespace Julo.Network