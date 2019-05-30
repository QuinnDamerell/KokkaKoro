using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class ListBotsResponse
    {
        // A list of all known games
        public List<KokkaKoroBot> Bots;
    }
}
