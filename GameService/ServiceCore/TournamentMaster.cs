using Newtonsoft.Json;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class TournamentMaster
    {
        readonly static TournamentMaster s_tournmentMaster = new TournamentMaster();
        public static TournamentMaster Get()
        {
            return s_tournmentMaster;
        }

        Dictionary<Guid, ServiceTournament> m_tournaments = new Dictionary<Guid, ServiceTournament>;

        public async Task<KokkaKoroResponse<object>> Create(string command, string userName)
        {
            KokkaKoroRequest<CreateTournamentOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<CreateTournamentOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse add or update bot", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.ReasonForCreation))
            {
                return KokkaKoroResponse<object>.CreateError("A reason for creation is required.");
            }
            int botsPerGame = request.CommandOptions.BotsPerGame;
            if(botsPerGame < 2 || botsPerGame > 4)
            {
                return KokkaKoroResponse<object>.CreateError("There must be 2-4 bots per game.");
            }
            int numberOfGames = request.CommandOptions.NumberOfGames;
            if (numberOfGames < 0 || numberOfGames > 1000)
            {
                return KokkaKoroResponse<object>.CreateError("The number of games must be between 0 and 1000.");
            }
            List<string> bots = request.CommandOptions.Bots;
            if(bots == null || bots.Count == 0)
            {
                bots = new List<string>();
                foreach(KokkaKoroBot b in await StorageMaster.Get().ListBots())
                {
                    bots.Add(b.Name);
                }
            }

            // Create it
            ServiceTournament tournament = new ServiceTournament(numberOfGames, botsPerGame, request.CommandOptions.ReasonForCreation, userName, bots);
            lock(m_tournaments)
            {
                m_tournaments.Add(tournament.GetId(), tournament);
            }

            // Start it
            tournament.Start();

            // Return
            return KokkaKoroResponse<object>.CreateResult(new CreateTournamentResponse() { Tournament = tournament.GetInfo() });
        }  
    }
}
