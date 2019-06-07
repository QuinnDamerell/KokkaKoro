using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class GetGameLogsOptions
    {
        // Required - The game id you want logs for.
        public Guid GameId;
    }
}
