
namespace Julo.Network
{

    public delegate DualServer CreateServerDelegate(Mode mode, CreateHostedClientDelegate createClient = null);
    public delegate DualClient CreateHostedClientDelegate(Mode mode, DualServer server);
    public delegate DualClient CreateRemoteClientDelegate(StartRemoteClientMessage startClientMessage);

    public class DNM
    {
        public const short SpecRole = 0;
        public const short FirstPlayerRole = 1;
    }

}
