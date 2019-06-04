using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{
    public enum StateUpdateType
    {
        [EnumMember(Value = "General")]
        General,

        [EnumMember(Value = "GameStart")]
        GameStart,

        [EnumMember(Value = "GameEnd")]
        GameEnd
    }

    public class GameStateUpdate
    {
        // The new state of the current games. Includes everything you would see on the game table if you
        // were playing in real life.
        public GameState State;

        // This indicates the type of state update.
        [JsonConverter(typeof(StringEnumConverter))]
        public StateUpdateType Type;

        // The reason for the game update.
        public string Reason;
    }
}
