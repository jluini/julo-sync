using UnityEngine;

using Julo.Users;

namespace Julo.Network
{
    public interface Player
    {
        uint GetId();

        int GetRole();

        string GetName();

        bool IsLocal();

    } // interface Player

} // namespace Julo.Network
