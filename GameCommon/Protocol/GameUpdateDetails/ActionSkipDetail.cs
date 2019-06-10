using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class ActionSkipDetails
    {
        // The action skipped
        public GameActionType SkippedAction;

        // The player index who skipped it.
        public int PlayerIndex;
    }
}
