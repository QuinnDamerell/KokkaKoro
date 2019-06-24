using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;
using KokkaKoro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using ServiceSdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceUtility
{
    class Program
    {
        static string Version = "1.0";

        static void Main(string[] args)
        {
            Logger log = new Logger();

            log.Info();
            log.Info($"***************************");
            log.Info($"*** Service Utility {Version} ***");
            log.Info($"***************************");
            log.Info();

            while (true)
            {
                // Create a new object.
                ServiceUtility ex = new ServiceUtility();

                // Call the main loop.
                try
                {
                    var task = ex.MainLoop(args);
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
        //
        // Add a port number here if you want to connect to a local service.
        // 
        int? localPort = null;

        public async Task<bool> MainLoop(string[] args)
        {
            Logger log = new Logger();

            // Create a new service every time.
            Service service = new Service();

            // Connect.
            log.Info($"Connecting to {(localPort == null ? "the Azure" : "A LOCAL")} service...");
            string address = (localPort.HasValue ? $"ws://localhost:{localPort.Value}" : null);
            await service.ConnectAsync(address);
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

            // If there are args, check them.
            if(args.Length > 0)
            {
                if(args[0].ToLower().Trim().Equals("uploadbot"))
                {
                    string path = null;
                    if(args.Length > 1)
                    {
                        path = args[1];
                    }
                    await AddOrUplaodBot(log, service, path);
                }
                else
                {
                    log.Info("Unknown arguments passed.");
                    log.Info("Usage:");
                    log.Info("   serviceutility.exe UploadBot <bot path>");
                    log.Info("");
                    Environment.Exit(1);
                }
            }

            while (true)
            {
                int command = GetCommand(log);

                switch (command)
                {
                    case 1:
                        await ListTournaments(log, service);
                        break;
                    case 2:
                        await CreateTournament(log, service);
                        break;
                    case 3:
                        await ListBots(log, service);
                        break;
                    case 4:
                        await AddOrUplaodBot(log, service);
                        break;
                    case 5:
                        await ListGames(log, service);
                        break;
                    case 6:
                        await GetGamesLogs(log, service);
                        break;
                    case 7:
                        await CreateGame(log, service);
                        break;
                }
            }
        }

        private int GetCommand(Logger log)
        {
            log.Info("");
            log.Info("");
            log.Info("*********************");
            log.Info("***   Main Menu   ***");
            log.Info("*********************");
            log.Info("");
            log.Info("****** TOURNAMENTS ******");
            log.SetIndent(1);
            log.Info("1) List Tournaments");
            log.Info("2) Create Tournament");
            log.SetIndent(0);
            log.Info("");
            log.Info("****** BOTS ******");
            log.SetIndent(1);
            log.Info("3) List Bots");
            log.Info("4) Add Or Upload Bot");
            log.SetIndent(0);
            log.Info("");
            log.Info("****** GAMES ******");
            log.SetIndent(1);
            log.Info("5) List Games");
            log.Info("6) Get Game Logs");
            log.Info("7) Create Game");
            log.SetIndent(0);
            log.Info("");
            log.Info("");
            return log.GetInt("Select a function", 1, 7);
        }

        private async Task AddOrUplaodBot(Logger log, Service service, string path = null)
        {
            BotUploader u = new BotUploader();
            await u.DoUpload(log, service, path);
        }

        private async Task ListGames(Logger log, Service service)
        {
            log.Info("Gathering games...");
            await GetGame(log, service, true, false);
        }

        private async Task ListBots(Logger log, Service service)
        {
            log.Info("Gathering bots...");
            await GetBot(log, service, true, false);
        }

        private async Task ListTournaments(Logger log, Service service)
        {
            log.Info("Gathering tournaments...");
            await GetTournament(log, service, true, false);
        }

        private async Task CreateTournament(Logger log, Service service)
        {
            log.Info();
            string name = log.GetString("What's the name of the tournament");
            int games = log.GetInt("How many games would you like to be played", 1, 1000);
            List<string> bots = null;
            if(log.GetDecission("Would you like to pick which bots will be in the tournament (if not, all bots on the service will be used)"))
            {
                bots = new List<string>();
                List<KokkaKoroBot> kokkBots = await service.ListBots();
                do
                {
                    // Ask for a bot.
                    KokkaKoroBot bot = await GetBot(log, service, false, true, kokkBots);
                    bots.Add(bot.Name);
                }
                while (bots.Count < 2 || log.GetDecission("Add another bot"));
            }
            int botsPerGame = log.GetInt("How many bots would you like in each game (if the bots selected count is less than this number repeats will be added)", 2, 4);
            log.Info();
            log.Info("Creating tournament...");
            KokkaKoroTournament tour = await service.CreateTournament(new CreateTournamentOptions() { Name = name, NumberOfGames = games, BotsPerGame = botsPerGame, Bots = bots });
            log.Info($"New tournament started! [{tour.Id}]");
        }

        private async Task GetGamesLogs(Logger log, Service service, bool saveLogs = false)
        {
            log.Info("Finding games...");
            KokkaKoroGame game = await GetGame(log, service, false, true);
            if(game == null)
            {
                return;
            }

            // Get the game logs.
            log.Info("Getting logs...");
            GetGameLogsResponse response = await service.GetGameLogs(new GetGameLogsOptions() { GameId = game.Id });

            log.Info();
            if (log.GetDecission("Would you like to print the logs or save them to your desktop (p=print; s=save)", "p", "s"))
            {
                // Print the game.
                log.Info();
                log.Info("Game Details:");
                log.IncreaseIndent();
                PrintGame(log, response.Game);
                log.DecreaseIndent();

                // Print the game log.
                log.Info("Game Log:");
                log.IncreaseIndent();
                log.Info(JsonConvert.SerializeObject(response.GameLog, Formatting.Indented));
                log.DecreaseIndent();

                // Print any bot logs.
                log.Info("Bot Logs:");
                foreach (KokkaKoroBotLog blog in response.BotLogs)
                {
                    log.Info($"Bot {blog.Player.BotDetails.Bot.Name} - {blog.Player.PlayerName}");
                    log.Info(blog.StdOut);
                    log.Info(blog.StdErr);
                }
                if (response.BotLogs.Count == 0)
                {
                    log.Info("None");
                }
            }
            else
            {
                log.Info();
                // Save the logs to the folder given.
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + $"/GameLogs-{game.GameName}-{game.Id}/";// GetString(log, "Enter a folder path to write the logs to.");
                Directory.CreateDirectory(path);
                log.Info($"Writing logs to {path}...");
                WriteFile($"{path}/GameLog.json", JsonConvert.SerializeObject(response.GameLog, Formatting.Indented));
                foreach (KokkaKoroBotLog blog in response.BotLogs)
                {
                    WriteFile($"{path}/{blog.Bot.Name}-{blog.Player.PlayerName}-stdout.txt", blog.StdOut);
                    WriteFile($"{path}/{blog.Bot.Name}-{blog.Player.PlayerName}-stderr.txt", blog.StdErr);
                }
                log.Info($"Logs written to {path}");
            }
        }

        private void WriteFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        private async Task CreateGame(Logger log, Service service)
        {
            string gameName = log.GetString("Enter a name for the game");

            log.Info("Creating game...");
            KokkaKoroGame game = await service.CreateGame(new CreateGameOptions()
            {
                GameName = gameName                 
            });

            log.Info($"New game created. [{game.Id}]");

            int count = 0;
            while(game.Players.Count < 4)
            {
                if (!log.GetDecission($"Would like you to add{(count == 0 ? "" : " another")} a bot"))
                {
                    break;
                }
                count++;

                // List and get the bots.
                KokkaKoroBot bot = await GetBot(log, service);
                if(bot == null)
                {
                    break;
                }
                string botName = log.GetString("Enter a friendly name for the bot");

                log.Info("Adding bot...");
                game = await service.AddBotToGame(new AddHostedBotOptions()
                {
                    BotName = bot.Name,
                    GameId = game.Id,
                    InGameName = botName
                });
                log.Info("Bot Added.");
            }

            if(log.GetDecission("Would you like to start the game now"))
            {
                log.Info("Starting game...");
                await service.StartGame(new StartGameOptions()
                {
                    GameId = game.Id
                });
                log.Info("Game Started!");
            }
            log.Info();
        }

        private void PrintGame(Logger log, KokkaKoroGame game)
        {
            log.Info($"{game.GameName}");
            log.IncreaseIndent();
            log.Info($"Id: {game.Id}");
            log.Info($"State: {game.State}");
            log.Info($"Created By: {game.CreatedBy}");
            log.Info($"Created: {game.Created.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")}");
            log.Info($"Started: {(game.Started.HasValue ? (game.Started.Value - game.Created).TotalSeconds + "s" : "")}");
            log.Info($"Game Engine Started: {(game.GameEngineStarted.HasValue ? (game.GameEngineStarted.Value - game.Created).TotalSeconds + "s" : "")}");
            log.Info($"Ended: {(game.Ended.HasValue ? (game.Ended.Value - game.Created).TotalSeconds + "s" : "")}");
            log.Info($"Has Winner: {game.HasWinner}");

            log.Info($"Leaderboard:");
            log.IncreaseIndent();
            if (game.Leaderboard != null)
            {
                foreach (KokkaKoroLeaderboardElement lb in game.Leaderboard)
                {
                    log.Info($"#{lb.Rank} - {lb.Player.Name}, {lb.LandmarksOwned} owned landmarks");
                }
            }
            else
            {
                log.Info("None.");
            }
            log.DecreaseIndent();

            log.Info($"Players:");
            log.IncreaseIndent();
            foreach (KokkaKoroPlayer player in game.Players)
            {
                PrintPlayer(log, player);
            }
            if (game.Players.Count == 0)
            {
                log.Info("None.");
            }
            log.DecreaseIndent();

            log.DecreaseIndent();
        }

        private void PrintPlayer(Logger log, KokkaKoroPlayer player)
        {
            log.Info($"{player.PlayerName}");
            log.IncreaseIndent();
            log.Info($"Is Bot: {player.IsBot}");
            if (player.IsBot)
            {
                log.Info($"Bot: {player.BotDetails.Bot.Name}");
                log.Info($"Version: {player.BotDetails.Bot.Major}.{player.BotDetails.Bot.Minor}.{player.BotDetails.Bot.Revision}");
                log.Info($"State: {player.BotDetails.State}");
                log.Info($"Error: {(String.IsNullOrWhiteSpace(player.BotDetails.IfErrorFatialError) ? "None" : player.BotDetails.IfErrorFatialError)}");
            }
            log.DecreaseIndent();
        }

        private async Task<KokkaKoroGame> GetGame(Logger log, Service service, bool showDetails = false, bool waitForDecision = true)
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
                if(showDetails)
                {
                    log.Info($"{count})");
                    log.IncreaseIndent();
                    PrintGame(log, game);
                    log.DecreaseIndent();
                }
                else
                {
                    log.Info($"{count}) {game.GameName} - {game.State}{(game.State == KokkaKoroGameState.Complete && !game.HasWinner ? " HAS ERROR" : "")} - Created {game.Created.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")} - {game.Id}");
                }
                count++;
            }          
            log.DecreaseIndent();
            if (waitForDecision)
            {
                int value = log.GetInt("Select a game", 1, count - 1);
                return games[value - 1];
            }
            return null;
        }

        private async Task<KokkaKoroBot> GetBot(Logger log, Service service, bool showDetails = false, bool waitForDecision = true, List<KokkaKoroBot> bots = null)
        {
            if (bots == null)
            {
                bots = await service.ListBots();
            }
            if (bots.Count == 0)
            {
                log.Info("There are no bots on the service!");
                return null;
            }
            log.Info();
            log.Info("Bots:");
            log.IncreaseIndent();
            int count = 1;
            foreach (KokkaKoroBot bot in bots)
            {
                log.Info($"{count}) {bot.Name} - Version {bot.Major}.{bot.Minor}.{bot.Revision}");
                count++;
            }
            log.DecreaseIndent();
            if (waitForDecision)
            {
                int value = log.GetInt("Select a bot", 1, count - 1);
                return bots[value - 1];
            }
            else
            {
                return null;
            }
        }

        private async Task<KokkaKoroTournament> GetTournament(Logger log, Service service, bool showDetails = false, bool waitForDecision = true)
        {
            List<KokkaKoroTournament> tours = await service.ListTournaments();
            if (tours.Count == 0)
            {
                log.Info("There are no tournaments on the service!");
                return null;
            }
            log.Info();
            log.Info("Tournaments:");
            log.IncreaseIndent();
            int count = 1;
            foreach (KokkaKoroTournament t in tours)
            {
                if(showDetails)
                {
                    log.Info($"{count})");
                    log.IncreaseIndent();
                    PrintTournament(log, t);
                    log.DecreaseIndent();
                }
                else
                {
                    log.Info($"{count}) {t.Id} - {t.Status} Created by {t.CreatedFor}");
                }
                count++;
            }
            log.DecreaseIndent();
            if (waitForDecision)
            {
                int value = log.GetInt("Select a tournament", 1, count - 1);
                return tours[value - 1];
            }
            else
            {
                return null;
            }
        }   
        
        private void PrintTournament(Logger log, KokkaKoroTournament t)
        {
            log.Info($"Name: {t.Name}");
            log.Info($"Id: {t.Id}");
            log.Info($"Status: {t.Status}");
            log.Info($"Created: {t.CreatedAt.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt")}");
            log.Info($"Ended: {(t.EndedAt.HasValue ? t.EndedAt.Value.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt") : "Still running.")}");
            log.Info($"Created By: {t.CreatedFor}");
            log.Info($"Error if failed: {(String.IsNullOrWhiteSpace(t.MessageIfError) ? "None" : t.MessageIfError)}");
            log.Info("Games:");
            int count = 1;
            log.IncreaseIndent();
            foreach(KokkaKoroGame game in t.Games)
            {
                string gameStr = $"{count}) {game.Id} - {game.State} - ";
                if (game.Leaderboard != null)
                {
                    foreach (KokkaKoroLeaderboardElement le in game.Leaderboard)
                    {
                        gameStr += $"#{le.Rank} {le.Player.Name}; ";
                    }
                }
                log.Info(gameStr);
                count++;
            }
            log.DecreaseIndent();
            log.Info("Results:");
            log.IncreaseIndent();
            t.Results.Sort((TournamentResult l, TournamentResult r) => { return r.Wins - l.Wins; });
            foreach (TournamentResult result in t.Results)
            {
                log.Info($"{result.BotName}: {result.Wins} wins, {result.Losses} losses, {result.Score} score, {result.InProgress} in progress, {result.Errors} errors");
            }
            log.DecreaseIndent();
        }
    }
}
