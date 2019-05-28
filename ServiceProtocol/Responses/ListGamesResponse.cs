using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class ListGamesResponse
    {
        // A list of all known games
        public List<KokkaKoroGame> Games;
    }
}
