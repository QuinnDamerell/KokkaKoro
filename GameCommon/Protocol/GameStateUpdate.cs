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
        // The game is on!
        [EnumMember(Value = "GameStart")]
        GameStart,

        // The game has ended!
        [EnumMember(Value = "GameEnd")]
        GameEnd,

        // A player has rolled the dice and generated results.
        [EnumMember(Value = "RollDiceResult")]
        RollDiceResult,

        // A player has committed their dice results.
        [EnumMember(Value = "CommitDiceResults")]
        CommitDiceResults,

        // A player has built a building.
        [EnumMember(Value = "BuildBuilding")]
        BuildBuilding,

        // A player has earned income.
        [EnumMember(Value = "EarnIncome")]
        EarnIncome,

        // A payment has been made from one player to another.
        [EnumMember(Value = "CoinPayment")]
        CoinPayment,

        // A stadium building collection was taken from all players.
        [EnumMember(Value = "StadiumCollection")]
        StadiumCollection,

        // A business center was invoked and building were swapped. 
        [EnumMember(Value = "BusinessCenterSwap")]
        BusinessCenterSwap,

        // A player has decided to skip an action.
        [EnumMember(Value = "ActionSkip")]
        ActionSkip,

        // A player ended their turn.
        [EnumMember(Value = "EndTurn")]
        EndTurn,

        // A player gets an extra turn.
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

        // A list of all players that are involved with invoking this update or who are directly effected by it.
        public List<int> InvolvedPlayerIndexes;

        // The reason for the game update.
        public string Reason;

        // Optionally, additional details about the update. The type of object can be determined by the StateUpdateType.
        public T Details;

        public static GameStateUpdate<T> Create(GameState state, StateUpdateType type, string reason, T details, int playerIndex)
        {
            List<int> players = new List<int>() { playerIndex };
            return new GameStateUpdate<T>() { State = state, Details = details, Reason = reason, Type = type, InvolvedPlayerIndexes = players};
        }

        public static GameStateUpdate<T> Create(GameState state, StateUpdateType type, string reason, T details, List<int> playerIndexes)
        {
            return new GameStateUpdate<T>() { State = state, Details = details, Reason = reason, Type = type, InvolvedPlayerIndexes = playerIndexes };
        }

        public static GameStateUpdate<T> Create(GameState state, StateUpdateType type, string reason, T details, bool allPlayers)
        {
            List<int> players = new List<int>();
            foreach(GamePlayer p in state.Players)
            {
                players.Add(p.PlayerIndex);
            }
            return new GameStateUpdate<T>() { State = state, Details = details, Reason = reason, Type = type, InvolvedPlayerIndexes = players };
        }
    }
}
