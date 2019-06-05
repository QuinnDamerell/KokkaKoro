using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class CommitDiceResultsDetails
    {
        // Who the result was for
        public int PlayerIndex;

        // The number of rolls taken.
        public int Rolls;

        // The results
        public List<int> DiceResults = new List<int>();
    }
}
