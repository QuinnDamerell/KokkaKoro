using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public class GamePlayer
    {
        // A friendly name for the player
        public string Name;

        // The service username of the player.
        public string UserName;

        // The current amount of coins they have.
        public int Coins;

        // A list of building owned by the player. The index into the list
        // is the building number and the value is the number owned.
        public List<int> OwnedBuildings = new List<int>();
    }
}
