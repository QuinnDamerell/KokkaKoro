﻿using Newtonsoft.Json;
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

        [EnumMember(Value = "SettingUp")]
        SettingUp,

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
        // The name of the tournament
        public string Name;

        // The id of the tournament
        public Guid Id;

        // The current status
        [JsonConverter(typeof(StringEnumConverter))]
        public TournamentStatus Status;

        // Indicates if the tournament is an official bot tournament.
        public bool IsOfficial;

        // When it was created UTC
        public DateTime CreatedAt;

        // If not null, when it was ended. UTC
        public DateTime? EndedAt;

        // The user name that created the tournament
        public string CreatedFor;

        // If the status is error, the message that stopped it.
        public string MessageIfError;

        // The number of total games that will be played.
        public int TotalGames;

        // The list of games in the given tournament
        public List<KokkaKoroGame> Games;

        // The list of bots playing and their results.
        public List<TournamentResult> Results;
    }
}
