using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public enum DiceCount
    {
        OneDice,
        TwoDice
    }

    public class RollDiceOptions
    {
        // Indicates the number of dice you would like to roll.
        public DiceCount Count;
    }
}
