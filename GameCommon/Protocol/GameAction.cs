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

        /// <summary>
        /// If you don't auto commit the result, the server will ask you if you want to commit the result after the results are returned.
        /// This is required because with a card you have the option to reroll the dice. But if you set the auto commit flag, the server
        /// will commit the result it generates instantly.
        /// </summary>
        /// <param name="numberOfDiceToRoll"></param>
        /// <param name="autoCommitResult"></param>
        /// <returns></returns>
        public static GameAction<object> CreateRollDiceAction(int numberOfDiceToRoll, bool autoCommitResult)
        {
            return new GameAction<object>() { Action = GameActionType.RollDice, Options = new RollDiceOptions() { DiceCount = numberOfDiceToRoll, AutoCommitResult = autoCommitResult } };
        }

        /// <summary>
        /// After you have been given dice results, this commits them for you turn.
        /// </summary>
        /// <returns></returns>
        public static GameAction<object> CreateCommitDiceResultAction()
        {
            return new GameAction<object>() { Action = GameActionType.CommitDiceResult, Options = null };
        }

        /// <summary>
        /// Indicates you want to buy a building.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <param name="autoEndTurn"></param>
        /// <returns></returns>
        public static GameAction<object> CreateBuildBuildingAction(int buildingIndex, bool autoEndTurn = true)
        {
            return new GameAction<object>() { Action = GameActionType.BuildBuilding, Options = new BuildBuildingOptions() { AutoEndTurn = autoEndTurn, BuildingIndex = buildingIndex } };
        }

        /// <summary>
        /// Indicates you want to end your turn.
        /// </summary>
        /// <returns></returns>
        public static GameAction<object> CreateEndTurnAction()
        {
            return new GameAction<object>() { Action = GameActionType.EndTurn, Options = null };
        }

        /// <summary>
        /// Indicates the player wants to respond to the tv station payout.
        /// </summary>
        /// <param name="playerIndexToTakeFrom"></param>
        /// <returns></returns>
        public static GameAction<object> CreateTvStationPayoutAction(int playerIndexToTakeFrom)
        {
            return new GameAction<object> { Action = GameActionType.TvStationPayout, Options = new TvStationPayoutOptions() { PlayerIndexToTakeFrom = playerIndexToTakeFrom } };
        }

        /// <summary>
        /// Indicates the player wants to respond to the business center swap.
        /// </summary>
        /// <param name="playerIndexToSwapWith"></param>
        /// <param name="bulidingIndexToGive"></param>
        /// <param name="buildingIndexToTake"></param>
        /// <param name="skipAction"></param>
        /// <returns></returns>
        public static GameAction<object> CreateBusinessCenterSwapAction(int playerIndexToSwapWith, int bulidingIndexToGive, int buildingIndexToTake, bool skipAction = false)
        {
            return new GameAction<object> { Action = GameActionType.BusinessCenterSwap, Options = new BusinessCenterSwapOptions() { PlayerIndexToSwapWith = playerIndexToSwapWith, BuildingIndexToGive = bulidingIndexToGive, BuildingIndexToTake = buildingIndexToTake, SkipAction = skipAction} };
        }

        /// <summary>
        /// Indicates that the bot has failed and is giving up. In this case the game will end and the bot will lose.
        /// </summary>
        /// <returns></returns>
        public static GameAction<object> CreateForfeitAction()
        {
            return new GameAction<object> { Action = GameActionType.Forfeit, Options = null };
        }
    }
}
