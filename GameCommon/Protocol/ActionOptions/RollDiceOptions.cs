using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public class RollDiceOptions
    {
        // Indicates the number of dice you would like to roll.
        public int DiceCount;

        // Since later on in the game you get the ability to re-roll the dice result,
        // the game engine will send the dice result back for all rolls unless this value
        // is set to true. If this value is set to true, whatever result is gotten will be
        // used for the turn.
        public bool AutoCommitResult = false;
    }
}
