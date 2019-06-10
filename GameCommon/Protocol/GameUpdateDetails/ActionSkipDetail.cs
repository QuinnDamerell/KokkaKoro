using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    // Indicates that a player decided to skip an action.
    public class ActionSkipDetails
    {
        // The action skipped
        public GameActionType SkippedAction;

        // The player index who skipped it.
        public int PlayerIndex;
    }
}
