using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class ListTournamentResponse
    {
        // A list of all known tournaments
        public List<KokkaKoroTournament> Tournaments;
    }
}
