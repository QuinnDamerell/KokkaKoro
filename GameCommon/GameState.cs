using GameCommon.StateHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public enum GameMode
    {
        Base
    }

    public class TurnState
    {
        // The index into the Players list of the current player.
        public int PlayerIndex;

        // How many rolls the current user has preformed.
        public int Rolls;

        // Indicates if the player has bought a building on this turn or not.
        public bool HasBougthBuilding;

        // The results of the dice that have been rolled, assuming they have been rolled.
        public List<int> DiceResults = new List<int>();

        public TurnState()
        {
            Clear(0);
        }

        public void Clear(int newPlayerIndex)
        {
            PlayerIndex = newPlayerIndex;
            Rolls = 0;
            DiceResults.Clear();
            HasBougthBuilding = false;
        }
    }

    public class GameState
    {
        // The game mode being played.
        public GameMode Mode;

        // A list of players in the game.
        // The players are ordered by turn order.
        public List<GamePlayer> Players = new List<GamePlayer>();

        // The current state of the current player's turn.
        public TurnState CurrentTurnState = new TurnState();

        // The market is the set of cards that are currently available for purchase.
        public Marketplace Market;

        //
        // Helpers
        //

        // Returns a state helper where questions are answered from the perspective of the given
        // username.
        public StateHelper GetStateHelper(string perspectiveUserName)
        {
            return new StateHelper(this, perspectiveUserName);
        }
    }
}
