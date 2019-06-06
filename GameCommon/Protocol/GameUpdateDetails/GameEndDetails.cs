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
        [EnumMember(Value = "PlayerWon")]
        PlayerWon,

        [EnumMember(Value = "GameTimeout")]
        GameTimeout,

        [EnumMember(Value = "Error")]
        Error
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
