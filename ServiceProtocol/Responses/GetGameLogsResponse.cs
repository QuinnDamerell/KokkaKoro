using GameCommon;
using GameCommon.Protocol;
using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class GetGameLogsResponse
    {
        // The current game state.
        public KokkaKoroGame Game;

        // If the game is running or has ended, the current state of the game.
        public GameState State;

        // If the game has started or has ended, the game log.
        public List<GameLog> GameLog;

        // If there are bots in the game, the current logs of the bots.
        public List<KokkaKoroBotLog> BotLogs;
    }
}
