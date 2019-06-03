using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects.Protocol
{
    public enum GameActionType
    {
        // The first action requested by the game host.
        [EnumMember(Value = "RollDice")]
        RollDice
    }

    class GameActionRequest
    {
    }
}
