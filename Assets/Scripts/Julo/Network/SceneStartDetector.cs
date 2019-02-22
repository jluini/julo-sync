using UnityEngine;

using Julo.Logging;

namespace Julo.Network
{
    public class SceneStartDetector : MonoBehaviour
    {
        void Start()
        {
            // scene started
            var dnm = DualNetworkManager.instance;
            if(!dnm)
            {
                Log.Debug("No DNM");
                return;
            }

            dnm.OnStartScene();
        }
    } // class SceneStartDetector

} // namespace Julo.Network