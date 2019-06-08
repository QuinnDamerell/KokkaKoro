using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class StadiumCollectionDetails
    {
        // The player index of who was given the coins.
        public int PlayerIndexPaidTo;

        // How many coins were taken from the players.
        public int MaxTakenFromEachPlayer;

        // How much was paid in total.
        public int TotalRecieved;
    }
}
