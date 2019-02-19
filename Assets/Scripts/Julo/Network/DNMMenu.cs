using UnityEngine;

using Julo.Logging;

namespace Julo.Network
{

    [RequireComponent(typeof(DualNetworkManager))]
    public class DNMMenu : MonoBehaviour
    {
        DualNetworkManager manager;

        void Awake()
        {
            manager = GetComponent<DualNetworkManager>();
        }

        public void StartOffline()
        {
            Log.Debug("START OFFLINE");
        }

    } // class DNMMenu

} // namespace Julo.Network