using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public enum GameEndReason
    {
        [EnumMember(Value = "Winner")]
        Winner,

        [EnumMember(Value = "PlayerForfeit")]
        PlayerForfeit,

        [EnumMember(Value = "GameTimeout")]
        GameTimeout,

        [EnumMember(Value = "RoundLimitHit")]
        RoundLimitReached,

        [EnumMember(Value = "GameEngineError")]
        GameEngineError
    }

    public class GameEndDetails
    {
        // The player index of the winner!
        // If there was no winner, this value is null.
        public int? PlayerIndex;

        // Why the game ended
        [JsonConverter(typeof(StringEnumConverter))]
        public GameEndReason Reason;
    }
}
