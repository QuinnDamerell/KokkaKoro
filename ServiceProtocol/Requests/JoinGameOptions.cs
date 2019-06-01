using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class JoinGameOptions
    {
        // Required - The game id you want to join.
        public Guid GameId;

        // Optional - The password for the game (if there is one)
        public string Password;
    }
}
