using UnityEngine;

using Julo.Users;

namespace Julo.Network
{
    public interface Player
    {
        uint GetId();

        //int GetConnection();
        //short GetControllerId();

        string GetName();
        int GetRole();

    } // interface Player

} // namespace Julo.Network
