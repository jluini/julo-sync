using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using Julo.Logging;
using Julo.Network;

namespace Julo.Users
{
    
    public class UserList : UserManager
    {

        public Dropdown dropdown;
        
        List<UserProfile> users = new List<UserProfile>();

        UserProfile activeUser = null;

        
        public override List<UserProfile> GetAllUsers()
        {
            return new List<UserProfile>(users);
        }

        public override UserProfile GetActiveUser()
        {
            return activeUser; //.GetData();
        }

        public override void Init()
        {
            // LoadUsers(); // load users from filesystem

            if(users.Count == 0)
            {
                AddNewUser();
            }
        }

        void AddNewUser()
        {
            UserProfile newUserProfile = new UserProfile("(new user)");

            users.Add(newUserProfile);

            var options = new List<Dropdown.OptionData>();

            Dropdown.OptionData newOption = new Dropdown.OptionData();
            newOption.text = newUserProfile.GetName();

            options.Add(newOption);

            dropdown.AddOptions(options);

            if(activeUser == null)
            {
                SetActiveUser(newUserProfile);
            }

        }

        public void SetActiveUser(UserProfile user)
        {
            activeUser = user;
        }
        
        public void OnAddNewUserClicked()
        {
            DualNetworkManager.instance.AddPlayerCommand();

            //Log.Debug("OnAddNewUserClicked: adding user");

            AddNewUser();
        }

        public void OnEditUserClicked()
        {
            Log.Debug("OnEditUserClicked: to be implemented");
        }

        public void OnPlayUserClicked()
        {
            Log.Debug("OnPlayUserClicked: to be implemented");
        }

    } // class UserList

} // namespace Julo.Users