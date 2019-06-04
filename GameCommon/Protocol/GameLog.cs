using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol
{
    // Every action and update that's taken in the game is expressed as a game log item, so they can all be
    // logged and the entire game can be visualized.
    public class GameLog
    {
        // The gameId where the log updates came from.
        public Guid GameId;

        // Not null if this log entry is a game state update.
        public GameStateUpdate<object> StateUpdate;

        // Not null if this log entry is a game action request to a player.
        public GameActionRequest ActionRequest;

        // Not null if this log entry is a user trying to take an action.
        public GameAction<object> Action;

        // Not null if this log entry is due to an error.
        public GameError Error;

        //<
        // Helpers
        // 
        public static GameLog CreateGameStateUpdate<T>(GameState state, StateUpdateType type, string message, T details)
        {
            return new GameLog() { StateUpdate = GameStateUpdate<object>.Create(state, type, message, details) };
        }

        public static GameLog CreateActionRequest(GameState state, List<GameActionType> actions)
        {
            return new GameLog() { ActionRequest = new GameActionRequest() { State = state, PossibleActions = actions } };
        }

        public static GameLog CreateAction(GameState state, GameAction<object> action)
        {
            return new GameLog() { Action = action } ;
        }

        public static GameLog CreateError(GameError error)
        {
            return new GameLog() { Error = error };
        }
    }
}
