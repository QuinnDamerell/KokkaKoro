using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Common
{
    public class KokkaKoroUser
    {
        // Required - The user name.
        public string UserName;

        // Required - The passcode associated with the user account.
        // Note: This passcode isn't handled like a password, 
        // so don't use any real passwords.
        public string Passcode;

        //
        // Helpers
        //
        public bool IsValid()
        {
            return !String.IsNullOrWhiteSpace(UserName) && !String.IsNullOrWhiteSpace(Passcode);
        }
    }
}
