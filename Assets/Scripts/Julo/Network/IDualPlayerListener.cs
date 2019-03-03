using Julo.Users;

namespace Julo.Network
{
    public interface IDualPlayerListener
    {

        void InitDualPlayer(Mode mode, bool isHosted = true, bool isLocal = true);

    }
}
