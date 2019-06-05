using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class RollDiceDetails
    {
        // Who the result was for
        public int PlayerIndex;

        // The results
        public List<int> DiceResults = new List<int>();
    }
}
