using UnityEngine;

namespace Julo.Network
{

    public delegate DualServer CreateServerDelegate(Mode mode);
    public delegate DualClient CreateHostedClientDelegate(Mode mode, DualServer server);
    public delegate DualClient CreateRemoteClientDelegate();

    public class DNM
    {
        public const int LocalConnectionId = 0;
        
        public const short SpecRole = 0;
        public const short FirstPlayerRole = 1;

        public static T GetPlayerAs<T>(DualPlayer player) where T : MonoBehaviour
        {
            //var b = (MonoBehaviour)player;
            var ret = player.GetComponent<T>();
            return ret;
        }
    }

}
