﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public class BusinessCenterSwapOptions
    {
        // Indicates which player you wish to swap with.
        public int PlayerIndexToSwapWith = -1;

        // The building you want to give the player.
        public int BuildingIndexToGive = -1;

        // The building you want to take from the player.
        public int BuildingIndexToTake = -1;

        // If true, nothing will be done. This might be necessary because there might not be a building you can swap with someone else.
        public bool SkipAction = false;
    }
}
