using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol.Common
{
    public enum TournamentStatus
    {
        [EnumMember(Value = "Created")]
        Created,

        [EnumMember(Value = "Running")]
        Running,

        [EnumMember(Value = "Complete")]
        Complete,

        [EnumMember(Value = "Error")]
        Error
    }

    public class TournamentResult
    {
        // The bot name
        public string BotName;

        // A score which considers 2nd and 3rd place as well as 1st.
        public int Score;

        // The number of wins they have.
        public int Wins;

        // The number of losses they have.
        public int Losses;

        // The win rate of the bot.
        public double WinRate;

        // How many games ended in error.
        public int Errors;

        // How many games are still in progress.
        public int InProgress;
    }

    public class KokkaKoroTournament
    {
        // The id of the tournament
        public Guid Id;

        // The current status
        [JsonConverter(typeof(StringEnumConverter))]
        public TournamentStatus Status;

        // A reason why it's created.
        public string Reason;

        // If the status is error, the message that stopped it.
        public string MessageIfError;

        // The user name that created the tournament
        public string CreatedFor;

        // The list of games in the given tournament
        public List<KokkaKoroGame> Games;

        // The list of bots playing and their results.
        public List<TournamentResult> Results;
    }
}
