using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class DiceRollDetails
    {
        // Who the result was for
        public int RolledForPlayerIndex;

        // The results
        public List<int> DiceResults = new List<int>();
    }
}
