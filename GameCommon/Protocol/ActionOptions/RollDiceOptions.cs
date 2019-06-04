using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public enum DiceCount
    {
        [EnumMember(Value = "OneDice")]
        OneDice,
        [EnumMember(Value = "TwoDice")]
        TwoDice
    }

    public class RollDiceOptions
    {
        // Indicates the number of dice you would like to roll.
        [JsonConverter(typeof(StringEnumConverter))]
        public DiceCount Count;
    }
}
