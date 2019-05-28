using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects
{



    class GameState
    {
        // A list of players in the game.
        // The players are ordered by turn order.
        public List<GamePlayer> Players = new List<GamePlayer>();

    }
}
