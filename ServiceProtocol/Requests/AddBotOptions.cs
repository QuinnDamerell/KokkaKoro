using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class AddBotOptions
    {
        // Required - The game id you want to add a bot to.
        public Guid GameId;

        // Required - The bot id you want to add.
        public Guid BotId;

        // Optional - The name of the bot for this game.
        public string BotName;

        // Optional - The password for the game (if there is one)
        public string Password;
    }
}
