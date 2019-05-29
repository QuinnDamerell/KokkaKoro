using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class StartGameOptions
    {
        // Required - The game id you want to add a bot to.
        public Guid GameId;

        // Optional - The password for the game (if there is one)
        public string Password;
    }
}
