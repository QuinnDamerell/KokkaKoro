using GameCommon.Protocol.ActionOptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{


    public class GameAction<T>
    {
        // The chosen action the player wants to preform.
        [JsonConverter(typeof(StringEnumConverter))]
        public GameActionType Action;

        // Given the actions type, this object can be optional or required options.
        public T Options;

        // 
        // Helpers
        //
        public static GameAction<object> CreateRollDiceAction(DiceCount numberOfDiceToRoll)
        {
            return new GameAction<object>() { Action = GameActionType.RollDice, Options = new RollDiceOptions() { Count = numberOfDiceToRoll } };
        }
    }
}
