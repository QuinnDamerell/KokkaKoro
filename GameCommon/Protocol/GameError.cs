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
        [EnumMember(Value = "Unknown")]
        Unknown,

        [EnumMember(Value = "LocalError")]
        LocalError,

        [EnumMember(Value = "NotPlayersTurn")]
        NotPlayersTurn,

        [EnumMember(Value = "PlayerUserNameNotFound")]
        PlayerUserNameNotFound,

        [EnumMember(Value = "UknownAction")]
        UknownAction,
    }

    public class GameError : Exception
    {
        // The type of error. This can help the client respond to it better.
        [JsonConverter(typeof(StringEnumConverter))]
        public ErrorTypes Type;

        // A string that describes the error
        new public string Message;

        // Whatever invoked the engine to produce this error, should it try again?
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
