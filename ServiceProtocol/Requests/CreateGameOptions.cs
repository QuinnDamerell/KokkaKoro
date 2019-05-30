using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class CreateGameOptions
    {
        // Required - Give your game a fun name!
        public string GameName;

        // Optional - Max number of players 
        public int? PlayerLimit;

        // Optional - Password to join
        public string Password;

        // Optional - The max time the game will be allowed to play
        public int? GameTimeLimitSeconds;

        // Optional - The max time a turn is allowed to take.
        public int? TurnTimeLmitSeconds;

        // Optional - The minium amount of time a turn can take.
        public int? MinTurnTimeSeconds;
    }
}
