using System.Collections.Generic;

using Julo.Logging;

namespace Julo.Users
{
    [System.Serializable]
    public class UserProfile
    {
        string name;

        public UserProfile(string name)
        {
            this.name = name;
        }

        public string GetName()
        {
            return name;
        }
    
    } // class UserProfile

} // namespace Julo.Users