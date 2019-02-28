using UnityEngine;

using Julo.Users;

namespace Julo.Network
{
    public interface IPlayer
    {
        uint GetId();


        bool IsLocal();

        // this is game-level

        //int GetRole();
        //string GetName();


    } // interface Player

} // namespace Julo.Network
