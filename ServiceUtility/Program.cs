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
        int? localPort = null;

        public async Task<bool> MainLoop()
        {
            Logger log = new Logger();

            // Create a new service every time.
            Service service = new Service();

            // Connect.
            log.Info($"Connecting to {(localPort == null ? "" : "LOCAL")} service...");
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
                    case 3:
                        await GetGamesLogs(log, service, true);
                        break;
                    case 4:
                        await CreateGame(log, service);
                        break;
                }
            }
        }

        private async Task ListGames(Logger log, Service service)
        {
            log.Info("Gathering games...");
            await GetGame(log, service, true, false);
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
            GetGameLogsResponse response = await service.GetGameLogs(new GetGameLogsOptions() { GameId = game.Id });

            if (!saveLogs)
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
                    log.Info($"Bot {blog.Player.BotName} - {blog.Player.PlayerName}");
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
                // Save the logs to the folder given.
                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);// GetString(log, "Enter a folder path to write the logs to.");
                log.Info($"Writing logs to {path}...");
                WriteFile($"{path}/{game.Id}-GameLog.json", JsonConvert.SerializeObject(response.GameLog, Formatting.Indented));
                foreach (KokkaKoroBotLog blog in response.BotLogs)
                {
                    WriteFile($"{path}/{game.Id}-{blog.Bot.Name}-{blog.Player.PlayerName}-stdout.txt", blog.StdOut);
                    WriteFile($"{path}/{game.Id}-{blog.Bot.Name}-{blog.Player.PlayerName}-stderr.txt", blog.StdErr);
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
            string gameName = GetString(log, "Enter a name for the game");

            log.Info("Creating game...");
            KokkaKoroGame game = await service.CreateGame(new CreateGameOptions()
            {
                GameName = gameName                 
            });

            log.Info($"New game created. [{game.Id}]");

            int count = 0;
            while(true)
            {
                if (!GetDecission(log, $"Would like you to add{(count == 0 ? "" : " another")} a bot"))
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
                string botName = GetString(log, "Enter a friendly name for the bot");

                log.Info("Adding bot...");
                await service.AddBotToGame(new AddHostedBotOptions()
                {
                    BotName = bot.Name,
                    GameId = game.Id,
                    InGameName = botName
                });
                log.Info("Bot Added.");
            }

            if(GetDecission(log, "Would you like to start the game now"))
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
            log.Info($"Player Limit: {game.PlayerLimit}");
            log.Info($"Created By: {game.CreatedBy}");
            log.Info($"Created: {game.Created}");
            log.Info($"Players:");
            log.IncreaseIndent();                       
            foreach (KokkaKoroPlayer player in game.Players)
            {
                PrintPlayer(log, player);
            }
            if(game.Players.Count == 0)
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
            log.Info("2) Print Game Logs");
            log.Info("3) Save Game Logs");
            log.Info("4) Create Game");
            log.Info("5) End Game");
            log.SetIndent(0);
            log.Info("");
            log.Info("##### BOTS  ######");
            log.SetIndent(1);
            log.Info("6) List Bots");
            log.SetIndent(0);
            log.Info("");
            log.Info("");
            return GetInt(log, "Select a function", 1, 4);
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
                    log.Info($"{count}) {game.GameName} - Created {(DateTime.UtcNow - game.Created).TotalMinutes} minutes ago. - {game.Id}");
                }
                count++;
            }          
            log.DecreaseIndent();
            if (waitForDecision)
            {
                int value = GetInt(log, "Select a game", 1, count - 1);
                return games[value - 1];
            }
            return null;
        }

        private async Task<KokkaKoroBot> GetBot(Logger log, Service service, bool waitForDecision = true)
        {
            List<KokkaKoroBot> bots = await service.ListBots();
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
                int value = GetInt(log, "Select a bot", 1, count - 1);
                return bots[value - 1];
            }
            else
            {
                return null;
            }
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

        private string GetString(Logger log, string message)
        {
            while (true)
            {
                log.Info($"{message}: ", false);
                string value = Console.ReadLine();
                if(!String.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
                log.Info("Invalid option, try again.");
            }
        }

        private bool GetDecission(Logger log, string message)
        {
            while (true)
            {
                log.Info($"{message}? [y or n] ", false);
                string value = Console.ReadLine();
                if (value.Trim().ToLower() == "y")
                {
                    return true;
                }
                if (value.Trim().ToLower() == "n")
                {
                    return false;
                }
                log.Info("Invalid option, try again.");
            }
        }
    }
}
