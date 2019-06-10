using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol.Common
{
    public enum KokkaKoroBotState
    {
        // The bot is waiting to be started
        [EnumMember(Value = "NotStarted")]
        NotStarted,

        // The process has been started, but the bot hasn't connected
        [EnumMember(Value = "Starting")]
        Starting,

        // The bot is joined to the game.
        [EnumMember(Value = "Joined")]
        Joined,

        // The process is being killed
        [EnumMember(Value = "Terminated")]
        Terminated,

        // The process is dead and done cleaning up
        [EnumMember(Value = "CleanedUp")]
        CleanedUp
    }

    public class KokkaKoroBotPlayer
    {
        // The bot details.
        public KokkaKoroBot Bot;

        // The current state of the bot.
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroBotState State;

        // If the bot ended in an error, this string will give details to why.
        public string IfErrorFatialError;
    }

    public class KokkaKoroPlayer
    {
        // Indicates if this player is a bot.
        public bool IsBot;

        // Indicates if the player is ready to play the game or not.
        public bool IsReady;

        // The name of this player, for this game.
        public string PlayerName;

        // If this player is a bot, more details on the bot.
        public KokkaKoroBotPlayer BotDetails;
    }
}
