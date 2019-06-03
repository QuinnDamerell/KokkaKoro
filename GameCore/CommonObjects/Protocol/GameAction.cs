using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCore.CommonObjects.Protocol
{


    class GameAction<T>
    {
        // The chosen action the player wants to preform.
        [JsonConverter(typeof(StringEnumConverter))]
        public GameActionType Action;
    }
}
