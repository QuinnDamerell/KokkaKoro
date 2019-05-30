using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class LoginResponse
    {
        // Returns the user name that was accepted.
        // (this will always be what was sent)
        public string UserName;
    }
}
