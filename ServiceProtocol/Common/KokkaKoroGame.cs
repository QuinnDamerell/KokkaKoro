using GameCommon.StateHelpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol.Common
{

    public enum KokkaKoroGameState
    {
        // The game is waiting for players to join and be started.
        [EnumMember(Value = "Lobby")]
        Lobby,

        // The game is started, but waiting for hosted bots to connect.
        [EnumMember(Value = "WaitingForHostedBots")]
        WaitingForHostedBots,

        // The game is in progress
        [EnumMember(Value = "InProgress")]
        InProgress,

        // The game is complete
        [EnumMember(Value = "Complete")]
        Complete
    }

    public class KokkaKoroGame
    {
        // The universal id of the game.
        public Guid Id;

        // The current state of the game
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroGameState State;

        // The name of the game
        public string GameName;

        // Who created it.
        public string CreatedBy;

        // The current players in the game.
        public List<KokkaKoroPlayer> Players = new List<KokkaKoroPlayer>();

        // Indicates if there's a password to join
        public bool HasPassword;

        // Indicates the max time the game can take.
        public double GameTimeLimitSeconds;

        // Optionally set, if the game ended in an error, this string gives context about why.
        public string IfFailedFatialError;

        // Indicates when the game was created.
        public DateTime Created;

        // Indicates when the game was sent the start command.
        public DateTime? Started;

        // Indicates when the game actually started, (since we have to connect bots initially)
        public DateTime? GameEngineStarted;

        // Indicates when the game ended.
        public DateTime? Eneded;

        // Indicates if the game has ended and has a winner.
        public bool HasWinner;

        // If the game is running or finished, the current leader board.
        public List<KokkaKoroLeaderboardElement> Leaderboard;
    }
}
