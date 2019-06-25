using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class CreateTournamentOptions
    {
        // Required - If set to true the other fields are ignored as this will be an official match.
        public bool IsOfficial;

        // Required - The reason why the tournament is being created.
        public string Name;

        // Required - the number of games the tournament should be.
        public int NumberOfGames;

        // Required - How many bots should be in each game.
        public int BotsPerGame;

        // Optional - A list of bot names to be played.
        public List<string> Bots;
    }
}
