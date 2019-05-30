using KokkaKoro;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceSdk;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceSdkExample
{
    class Program
    {
        static AutoResetEvent m_doneEvent;

        static void Main(string[] args)
        {
            m_doneEvent = new AutoResetEvent(false);
            DoWorkWrapper();
            m_doneEvent.WaitOne();
        }

        public static async void DoWorkWrapper()
        {
            await DoWork();
            m_doneEvent.Set();
        }

        public static async Task DoWork()
        {
            // All of the SDK will throw if something goes wrong.
            try
            {
                // Create a new service object
                Service kokkaKoroService = new Service();

                // Turn on debugging
                kokkaKoroService.SetDebugging(false);

                // Connect to the service
                Console.WriteLine($"Connecting to service...");
                await kokkaKoroService.ConnectAsync(51052);
                Console.WriteLine($"");

                // Get a list of the current games.
                List<KokkaKoroGame> games = await kokkaKoroService.ListGames();
                Console.WriteLine($"Current games:");
                foreach (KokkaKoroGame game in games)
                {
                    Console.WriteLine($"  {game.GameName} - Players {game.Players.Count}, {game.Id}");
                }
                Console.WriteLine($"");
                Console.WriteLine($"");

                // Get a list of the bots.
                List<KokkaKoroBot> bots = await kokkaKoroService.ListBots();
                Console.WriteLine($"Current bots:");
                foreach (KokkaKoroBot bot in bots)
                {
                    Console.WriteLine($"  {bot.Name} - {bot.Major}.{bot.Minor}.{bot.Revision}");
                }
                Console.WriteLine($"");
                Console.WriteLine($"");

                // Create a new game
                CreateGameOptions options = new CreateGameOptions()
                {
                    GameName = "SDK Example Game",
                    CreatedBy = "Example SDK"
                };
                KokkaKoroGame newGame = await kokkaKoroService.CreateGame(options);
                Console.WriteLine($"Game Created: {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");

                // Add bots to the game we just made.
                AddBotOptions botsOpts = new AddBotOptions()
                {
                    BotName = "TestBot",
                    InGameName = "Quinn",
                    GameId = newGame.Id
                };
                newGame = await kokkaKoroService.AddBotToGame(botsOpts);
                Console.WriteLine($"Bot {botsOpts.BotName} added! : {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");

                botsOpts = new AddBotOptions()
                {
                    BotName = "TestBot",
                    InGameName = "Marilyn",
                    GameId = newGame.Id
                };
                newGame = await kokkaKoroService.AddBotToGame(botsOpts);
                Console.WriteLine($"Bot {botsOpts.BotName} added! : {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");

                // Now start the game.
                StartGameOptions startGameOpts = new StartGameOptions()
                {
                    GameId = newGame.Id
                };
                newGame = await kokkaKoroService.StartGame(startGameOpts);
                Console.WriteLine($"Game Started! {newGame.GameName} {newGame.State} - Players {newGame.Players.Count}, {newGame.Id}");
            }
            catch (KokkaKoroException e)
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("!! ERROR !!");
                Console.WriteLine($"   Message:{e.Message}");
                Console.WriteLine($"   Was From Service? {e.IsFromService()}");
            }
            catch(Exception e)
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("!! ERROR !!");
                Console.WriteLine($"   Message:{e.Message}");
            }
        }
    }
}
