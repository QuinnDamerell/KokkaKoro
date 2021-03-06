﻿using System;
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

        // Tells the server you want to use the result generated.
        [EnumMember(Value = "CommitDiceResult")]
        CommitDiceResult,

        // Tells the server the client wishes to build a building.
        [EnumMember(Value = "BuildBuilding")]
        BuildBuilding,

        // Tells the server the client has decided who to take the TV Station payout from.
        [EnumMember(Value = "TvStationPayout")]
        TvStationPayout,

        // Tells the server the client has decided the player and building they want to swap due to a business center activation.
        [EnumMember(Value = "BusinessCenterSwap")]
        BusinessCenterSwap,

        // Tells the server the client wishes to end the turn.
        [EnumMember(Value = "EndTurn")]
        EndTurn,

        // If the player hits and error they can't recover from, they can forfeit. This will end the game and they will lose.
        [EnumMember(Value = "Forfeit")]
        Forfeit
    }

    public class GameActionRequest
    {
        // The current state of the game.
        public GameState State;

        // A list of possible actions the player can take right now.
        public List<GameActionType> PossibleActions;
    }
}
