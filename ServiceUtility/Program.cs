using KokkaKoro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using ServiceSdk;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger log = new Logger();
            while(true)
            {
                // Create a new object.
                ServiceUtility ex = new ServiceUtility();

                // Call the main loop.
                try
                {
                    var task = ex.MainLoop();
                    task.Wait();

                    // If it returns false exit.
                    if (!task.Result)
                    {
                        break;
                    }
                }
                catch(Exception e)
                {
                    log.SetIndent(0);
                    log.Info();
                    log.Info();
                    log.Error($"Exception thrown from main loop.", e);
                }

                log.SetIndent(0);

                // Whenever it fails, spin again.
                log.Info("Failed to connect, sleeping for 1s and trying again.");
                Thread.Sleep(1000);
            }         
        }
    }

    class ServiceUtility
    {
        int? localPort = 27699;

        public async Task<bool> MainLoop()
        {
            Logger log = new Logger();

            // Create a new service every time.
            Service service = new Service();

            // Connect.
            log.Info($"Connecting to {(localPort == null ? "" : "LOCAL")} service...");
            await service.ConnectAsync(localPort);
            log.Info($"Connected.");

            // Login
            log.Info($"Logging in...");
            LoginOptions loginOptions = new LoginOptions()
            {
                User = new KokkaKoroUser
                {
                    UserName = "ServiceUtility",
                    Passcode = "AGoodPasscode"
                }
            };
            await service.Login(loginOptions);
            log.Info($"Logged in.");

            while(true)
            {
                int command = GetCommand(log);

                switch (command)
                {
                    case 1:
                        await ListGames(log, service);
                        break;
                    case 2:
                        await GetGamesLogs(log, service);
                        break;
                }
            }
        }

        private async Task ListGames(Logger log, Service service)
        {
            log.Info("Gathering games...");
            List<KokkaKoroGame> games = await service.ListGames();
            log.Info("Games:");
            log.IncreaseIndent();
            foreach (KokkaKoroGame game in games)
            {
                PrintGame(log, game);
            }
            if (games.Count == 0)
            {
                log.Info("No Games.");
            }
            log.DecreaseIndent();
        }

        private async Task GetGamesLogs(Logger log, Service service)
        {
            log.Info("Finding games...");
            Guid? gameId = await GetGameId(log, service);
            if(!gameId.HasValue)
            {
                return;
            }

            // Get the game logs.
            GetGameLogsResponse response = await service.GetGameLogs(new GetGameLogsOptions() { GameId = gameId.Value });

            // Print the game.
            PrintGame(log, response.Game);

            // Print the game log.
            log.Info(JsonConvert.SerializeObject(response.GameLog, Formatting.Indented));

            // Print any bot logs.
            foreach(KokkaKoroBotLog blog in response.BotLogs)
            {
                log.Info($"Bot {blog.Player.BotName} - {blog.Player.PlayerName}");
                log.Info(blog.StdOut);
                log.Info(blog.StdErr);
            }
        }

        private void PrintGame(Logger log, KokkaKoroGame game)
        {
            log.Info($"{game.GameName}");
            log.IncreaseIndent();
            log.Info($"Id: {game.Id}");
            log.Info($"State: {game.State}");
            log.Info($"Player Limit: {game.PlayerLimit}");
            log.Info($"Created By: {game.CreatedBy}");
            log.Info($"Created: {game.Created}");
            log.Info($"Players:");
            log.IncreaseIndent();                       
            foreach (KokkaKoroPlayer player in game.Players)
            {
                PrintPlayer(log, player);
            }
            log.DecreaseIndent();
            log.DecreaseIndent();
        }

        private void PrintPlayer(Logger log, KokkaKoroPlayer player)
        {
            log.Info($"{player.PlayerName}");
            log.IncreaseIndent();
            log.Info($"Is Bot: {player.IsBot}");
            log.Info($"Bot Name: {player.BotName}");
            log.DecreaseIndent();
        }

        private int GetCommand(Logger log)
        {
            log.Info("");
            log.Info("");
            log.Info("##### GAMES ######");
            log.SetIndent(1);
            log.Info("1) List Games");
            log.Info("2) Get Game Logs");
            log.Info("3) End Game");
            log.SetIndent(0);
            log.Info("");
            log.Info("##### BOTS  ######");
            log.SetIndent(1);
            log.Info("   4) List Bots");
            log.SetIndent(0);
            log.Info("");
            log.Info("");
            return GetInt(log, "Select a function", 1, 4);
        }

        private async Task<Guid?> GetGameId(Logger log, Service service)
        {
            List<KokkaKoroGame> games = await service.ListGames();
            if (games.Count == 0)
            {
                log.Info("There are no games on the service!");
                return null;
            }
            log.Info();
            log.Info("Games:");
            log.IncreaseIndent();
            int count = 1;
            foreach(KokkaKoroGame game in games)
            {
                log.Info($"{count}) {game.GameName} - Created {(DateTime.UtcNow - game.Created).TotalMinutes} minutes ago. - {game.Id}");
                count++;
            }          
            log.DecreaseIndent();
            int value = GetInt(log, "Select a game", 1, count - 1);
            return games[value-1].Id;
        }

        private int GetInt(Logger log, string message, int min, int max)
        {
            while(true)
            {
                log.Info($"{message}: ", false);
                string value = Console.ReadLine();
                if(int.TryParse(value, out var result))
                {
                    if(result >= min && result <= max)
                    {
                        return result;
                    }
                }
                log.Info("Invalid option, try again.");
            }
        }  

    }
}
