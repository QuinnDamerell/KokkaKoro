using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{
    public enum ErrorTypes
    {
        // Who knows.
        [EnumMember(Value = "Unknown")]
        Unknown,

        // The game is in an invalid state.
        [EnumMember(Value = "InvalidState")]
        InvalidState,

        // Something happened locally on the client.
        [EnumMember(Value = "LocalError")]
        LocalError,

        // It's not this player's turn to make take actions.
        [EnumMember(Value = "NotPlayersTurn")]
        NotPlayersTurn,

        // The player wasn't found in the player list for this game.
        [EnumMember(Value = "PlayerUserNameNotFound")]
        PlayerUserNameNotFound,

        // The requested action has unknown.
        [EnumMember(Value = "UknownAction")]
        UknownAction,

        // The requested action had invalid options (or no options)
        [EnumMember(Value = "InvalidActionOptions")]
        InvalidActionOptions,

        // The requested build action failed because there weren't enough funds.
        [EnumMember(Value = "NotEnoughFunds")]
        NotEnoughFunds,

        // The requested build action failed because the building isn't available in the marketplace.
        [EnumMember(Value = "NotAvailableInMarketplace")]
        NotAvailableInMarketplace,

        // The requested build action failed because the user has the max number of building allowed per player.
        [EnumMember(Value = "PlayerMaxBuildingLimitReached")]
        PlayerMaxBuildingLimitReached,

        // The requested action is invalid given the current game state.
        [EnumMember(Value = "InvalidStateToTakeAction")]
        InvalidStateToTakeAction,

        // The requested action can't be done because there are pending special actions.
        [EnumMember(Value = "PendingSpecialActivations")]
        PendingSpecialActivations,

        // The requested action can't be applied to yourself.
        [EnumMember(Value = "ActionCantBeTakenOnSelf")]
        ActionCantBeTakenOnSelf,

        // The game has already ended.
        [EnumMember(Value = "GameEnded")]
        GameEnded,

    }

    public class GameError
    {
        // The type of error. This can help the client respond to it better.
        [JsonConverter(typeof(StringEnumConverter))]
        public ErrorTypes Type;

        // A string that describes the error
        public string Message;

        // Can the player who invoked this error try again?
        public bool CanTryAgain;

        // The current state of the game.
        public GameState State;

        // 
        // Helpers
        // 
        public static GameError Create(GameState state, ErrorTypes type, string error, bool canTryAgain)
        {
            return new GameError() { Message = error, Type = type, State = state, CanTryAgain = canTryAgain };
        }
    }
}
