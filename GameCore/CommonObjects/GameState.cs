using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects
{
    public enum GameMode
    {
        Base
    }

    public enum TurnState
    {
        WaitingOnRoll,
        WaitingOnBuyDecision
    }

    public class GameState
    {
        // The game mode being played.
        public GameMode Mode;

        // A list of players in the game.
        // The players are ordered by turn order.
        public List<GamePlayer> Players = new List<GamePlayer>();

        // An index to the player who's turn it currently is.
        public int CurrentPlayerIndex = 0;

        // The current state of the current player.
        public TurnState TurnState;

        // The market is the set of cards that are currently available for purchase.
        public Marketplace Market;
    }
}
