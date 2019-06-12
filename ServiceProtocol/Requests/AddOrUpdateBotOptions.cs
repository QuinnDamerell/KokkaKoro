using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class AddOrUpdateBotOptions
    {
        // Required - The bot details. 
        public KokkaKoroBot Bot;

        // Required - A zip file with all of the bot's logic in it.
        public string Base64EncodedZipedBotFiles;
    }
}
