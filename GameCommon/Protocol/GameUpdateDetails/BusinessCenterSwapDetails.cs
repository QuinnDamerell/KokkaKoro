using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    // Indicates that a business center was invoked and there was a swap done.
    public class BusinessCenterSwapDetails
    {
        // The player indexes who got buildings messed with.
        public int PlayerIndex1;
        public int PlayerIndex2;

        // The building indexes that each player got. 
        // Obviously, the inverse is which building each player lost.
        public int BudingIndexPlayer1Recieved;
        public int BudingIndexPlayer2Recieved;
    }
}
