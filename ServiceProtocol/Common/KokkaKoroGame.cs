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

        // The game is in progress
        [EnumMember(Value = "InProgress")]
        InProgress,

        // The game is complete
        [EnumMember(Value = "Complete")]
        Complete
    }

    public class KokkaKoroGame
    {
        // The univeral id of the game.
        public Guid Id;

        // The current state of the game
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroGameState State;

        // The name of the game
        public string GameName;

        // Who created it.
        public string CreatedBy;

        // How many players can join
        public int PlayerLimit;

        // Indicates if there's a password to join
        public bool HasPassword;

        // Indicates the max time a turn can take.
        public double TurnTimeLimitSeconds;

        // Indicates the mininum time a turn can take
        public double MinTurnTimeLimitSeconds;

        // Indicates the max time the game can take.
        public double GameTimeLimitSeconds;

        // Indicates when the game was created.
        public DateTime Created;
    }
}
