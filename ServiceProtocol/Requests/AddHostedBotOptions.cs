using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class AddHostedBotOptions
    {
        // Required - The game id you want to add a bot to.
        public Guid GameId;

        // Required - The name of the bot you want to add.
        public string BotName;

        // Required - A fun name of this bot for in this game.
        public string InGameName;

        // Optional - The password for the game (if there is one)
        public string Password;
    }
}
