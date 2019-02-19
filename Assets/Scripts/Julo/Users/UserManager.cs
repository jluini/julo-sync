using System.Collections.Generic;

using UnityEngine;

namespace Julo.Users
{

    public abstract class UserManager : MonoBehaviour
    {

        public abstract void Init();
        public abstract List<UserProfile> GetAllUsers();
        public abstract UserProfile GetActiveUser();
        //public abstract bool DeleteUser(UserData user);


    } // class UserManager

} // namespace Julo.Users