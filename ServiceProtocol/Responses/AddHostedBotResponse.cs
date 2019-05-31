using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class AddHostedBotResponse
    {
        // The updated details of the game we added a bot to.
        public KokkaKoroGame Game;

        // Indicates if the bot was loaded from the local cache.
        public bool WasInCache;

        // The bot details
        public KokkaKoroBot Bot;
    }
}
