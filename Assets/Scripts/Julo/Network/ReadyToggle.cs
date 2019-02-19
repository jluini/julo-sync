using UnityEngine;

namespace Julo.Network
{
    public class ReadyToggle : MonoBehaviour
    {

        public void OnValueChanged(bool newValue)
        {
            DualNetworkManager.instance.ClientSetReady(newValue);
        }

    } // class ReadyToggle

} // namespace Julo.Network