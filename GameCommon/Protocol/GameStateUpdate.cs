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
        [EnumMember(Value = "GameStart")]
        GameStart,

        [EnumMember(Value = "GameEnd")]
        GameEnd,

        [EnumMember(Value = "RollDiceResult")]
        RollDiceResult,

        [EnumMember(Value = "CommitDiceResults")]
        CommitDiceResults,

        [EnumMember(Value = "BuildBuilding")]
        BuildBuilding,

        [EnumMember(Value = "EarnIncome")]
        EarnIncome,

        [EnumMember(Value = "CoinPayment")]
        CoinPayment,

        [EnumMember(Value = "EndTurn")]
        EndTurn,

        [EnumMember(Value = "ExtraTurn")]
        ExtraTurn,
    }

    public class GameStateUpdate<T>
    {
        // The new state of the current games. Includes everything you would see on the game table if you
        // were playing in real life.
        public GameState State;

        // This indicates the type of state update.
        [JsonConverter(typeof(StringEnumConverter))]
        public StateUpdateType Type;

        // The reason for the game update.
        public string Reason;

        // Optionally, additional details about the update. The type of object can be determined by the StateUpdateType.
        public T Details;

        public static GameStateUpdate<T> Create(GameState state, StateUpdateType type, string reason, T details)
        {
            return new GameStateUpdate<T>() { State = state, Details = details, Reason = reason, Type = type };
        }
    }
}
