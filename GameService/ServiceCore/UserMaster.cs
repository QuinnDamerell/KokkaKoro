using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class UserMaster
    {
        static UserMaster s_instance = new UserMaster();
        public static UserMaster Get()
        {
            return s_instance;
        }

        Dictionary<string, string> m_users = new Dictionary<string, string>();

        public bool ValidateUserPasscode(string userName, string passcode)
        { 
            // TODO - someone. The idea here is to just keep track of past users and thier passcodes
            // maybe store a file on the blob storage with them. (hashed)
            return true;
        }
    }
}
