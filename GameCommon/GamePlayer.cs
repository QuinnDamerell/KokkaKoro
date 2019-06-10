using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    /// <summary>
    /// Represents a player in the game.
    /// </summary>
    public class GamePlayer
    {
        /// <summary>
        /// The player index for this player, just for reference.
        /// </summary>
        public int PlayerIndex;

        /// <summary>
        /// A friendly name for the player
        /// </summary>
        public string Name;

        /// <summary>
        /// The service username of the player.
        /// </summary>
        public string UserName;

        /// <summary>
        /// The current amount of coins they have.
        /// </summary>
        public int Coins;

        /// <summary>
        /// A list of building owned by the player. The index into the list
        /// is the building number and the value is the number owned.
        /// </summary>
        public List<int> OwnedBuildings = new List<int>();
    }
}
