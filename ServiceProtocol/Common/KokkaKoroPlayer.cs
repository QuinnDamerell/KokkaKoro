using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol.Common
{
    public class KokkaKoroPlayer
    {
        // Indicates if this player is a bot.
        public bool IsBot;

        // The name of this player, for this game.
        public string PlayerName;

        // If this player is a bot, the bot name
        public string BotName;
    }
}
