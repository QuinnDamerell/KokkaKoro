using GameCommon;
using GameCommon.Protocol;
using System;

namespace GameCommon
{
    /// <summary>
    /// Used in the game engine to generate errors.
    /// </summary>
    public class GameErrorException : Exception
    {
        public GameErrorException(string msg) 
            : base(msg)
        { Message = msg; }

        // The type of error. This can help the client respond to it better.
        public ErrorTypes Type;

        // A string that describes the error
        new public string Message;

        // Whatever invoked the engine to produce this error, should it try again?
        public bool CanTryAgain;

        // Is this fatal to the game.
        public bool IsFatal;

        // The current state of the game.
        public GameState State;

        // 
        // Helpers
        // 
        public static GameErrorException Create(GameState state, ErrorTypes type, string error, bool canTryAgain, bool isFatal = false)
        {
            return new GameErrorException(error) { Type = type, State = state, CanTryAgain = canTryAgain, IsFatal = isFatal  };
        }

        public GameError GetGameError()
        {
            return new GameError() { Message = Message, State = State, Type = Type, CanTryAgain = CanTryAgain };
        }
    }
}
