using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class LoginOptions
    {
        public KokkaKoroUser User;

        // The game and protocol version of this client.
        public int GameVersion;
        public int ProtocolVersion;
    }
}
