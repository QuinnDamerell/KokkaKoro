using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects
{
    public enum GameMode
    {
        Base
    }


    public class GameState
    {
        // The game mode being played.
        public GameMode Mode;

        // The current player number that's active.
        public int ActivePlayer;

        // A list of players in the game.
        // The players are ordered by turn order.
        public List<GamePlayer> Players = new List<GamePlayer>();

        // The market is the set of cards that are currently avaible for purchase.
        MarketPlace Market;



    }
}
