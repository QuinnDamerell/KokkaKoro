using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class SetUserNameOptions
    {
        // Required - The game id you want to add a bot to.
        public string UserName;

        // Optional - The passcode assoicated with the user account we are tyring to access.
        // If no passcode is given, this account can be assumed by anyone.
        // Note this passcode isn't handled like a password, so don't use any real passwords.
        public string Passcode;
    }
}
