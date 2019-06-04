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

        // If you don't auto commit the result, the server will ask you if you want to commit the result after the results are returned.
        // This is required because with a card you have the option to reroll the dice. But if you set the auto commit flag, the server
        // will commit the result it generates instantly.
        public static GameAction<object> CreateRollDiceAction(int numberOfDiceToRoll, bool autoCommitResult)
        {
            return new GameAction<object>() { Action = GameActionType.RollDice, Options = new RollDiceOptions() { DiceCount = numberOfDiceToRoll, AutoCommitResult = autoCommitResult } };
        }

        // After you have been given dice results, this commits them for you turn.
        public static GameAction<object> CreateCommitDiceResult()
        {
            return new GameAction<object>() { Action = GameActionType.CommitDiceResult, Options = null };
        }
    }
}
