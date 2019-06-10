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
        public int DiceCount = -1;

        // Since in the game you can get the ability to re-roll the dice result,
        // the game engine will send the dice result back for all rolls unless this value
        // is set to true. 
        // If this value is set to TRUE, the dice result will be committed when generated.
        // If this value is set to FALSE, the dice result will be returned and the player will
        // need to call the commit action manually.
        public bool AutoCommitResult = false;
    }
}
