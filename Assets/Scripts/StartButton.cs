using UnityEngine;

using Julo.Network;

public class StartButton : MonoBehaviour {
    public void OnClick()
    {
        DualNetworkManager.instance.TryToStartGame();
    }
}
