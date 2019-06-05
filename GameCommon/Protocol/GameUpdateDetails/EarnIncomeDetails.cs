using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class EarnIncomeDetails
    {
        // The player index who earned the coins.
        public int PlayerIndex;

        // How much was earned.
        public int Earned;

        // From which building
        public int BuildingIndex;
    }
}
