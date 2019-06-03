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

        // Not null if this log entry is a game update.
        public GameUpdate Update;

        // Not null if this log entry is a game action request to a player.
        public GameActionRequest ActionRequest;         

        //
        // Helpers
        // 
        public static GameLog CreateGameUpdate(GameState state, string message)
        {
            return new GameLog() { Update = new GameUpdate() { State = state, UpdateText = message } };
        }

        public static GameLog CreateActionRequest(GameState state, List<GameActionType> actions)
        {
            return new GameLog() { ActionRequest = new GameActionRequest() { State = state, PossibleActions = actions } };
        }
    }
}
