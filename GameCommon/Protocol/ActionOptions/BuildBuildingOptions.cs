using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public class BuildBuildingOptions
    {
        // Indicates the building index to be built.
        public int BuildingIndex = -1;

        // Since this is the last action a user can take, if this
        // flag is set the players turn will be ended. If not, the player
        // can manually end their turn with the CompleteTurn action.
        public bool AutoEndTurn = false;
    }
}
