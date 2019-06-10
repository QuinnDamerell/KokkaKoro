using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class CreateGameOptions
    {
        // Required - Give your game a fun name!
        public string GameName;

        // Optional - Password to join
        public string Password;

        // Optional - The max time the game will be allowed to play
        public int? GameTimeLimitSeconds;
    }
}
