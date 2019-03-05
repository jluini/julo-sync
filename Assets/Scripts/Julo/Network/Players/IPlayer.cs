namespace Julo.Network
{
    // TODO unificate with IDualPlayer?
    public interface IPlayer
    {

        uint PlayerId();

        int ConnectionId();

        short ControllerId();

        bool IsLocal();

    } // interface IPlayer

} // namespace Julo.Network
