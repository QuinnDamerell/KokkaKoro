using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{
    public enum GameActionType
    {
        // The first action requested by the game host.
        [EnumMember(Value = "RollDice")]
        RollDice
    }

    public class GameActionRequest
    {
        // The current state of the game.
        public GameState State;

        // A list of possible actions the player can take right now.
        public List<GameActionType> PossibleActions;
    }
}
