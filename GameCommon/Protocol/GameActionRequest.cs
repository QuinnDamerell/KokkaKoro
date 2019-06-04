using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{
    public enum GameActionType
    {
        // Tells the server to roll or re-roll the dice.
        [EnumMember(Value = "RollDice")]
        RollDice,

        // Tells the server the client wishes to build a building.
        [EnumMember(Value = "BuyBuilding")]
        BuyBuilding,
    }

    public class GameActionRequest
    {
        // The current state of the game.
        public GameState State;

        // A list of possible actions the player can take right now.
        public List<GameActionType> PossibleActions;
    }
}
