using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol
{
    public class GameUpdate
    {
        // The new state of the current games. Includes everything you would see on the game table if you
        // were playing in real life.
        public GameState State;

        // The reason for the game update.
        public string UpdateText;
    }
}
