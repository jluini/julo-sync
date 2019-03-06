namespace Julo.Network
{
    public interface IPlayer
    {

        int ConnectionId();

        short ControllerId();

        bool IsLocal();

    } // interface IPlayer

} // namespace Julo.Network
