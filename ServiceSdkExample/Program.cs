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
        static void Main(string[] args)
        {
            AutoResetEvent doneEvent = new AutoResetEvent(false);
            Example ex = new Example();
            ex.WorkWrapper(doneEvent);
            doneEvent.WaitOne();
        }
    }

    class Example
    {
        int? localPort = 5000;

        public async void WorkWrapper(AutoResetEvent doneEvent)
        {
            await DoWork();
            doneEvent.Set();
        }

        public async Task DoWork()
        {
            // All of the SDK will throw if something goes wrong.
            try
            {
                // Create a new service object. If the main websocket is every disconnected, it's required
                // to make a new service object and reconnect.
                Service kokkaKoroService = new Service();

                // Turn on debugging if desired.
                kokkaKoroService.SetDebugging(false);

                // Connect to the service
                Print("");
                Print($"Connecting to {(localPort == null ? "" : "LOCAL")} service...");
                await kokkaKoroService.ConnectAsync(localPort);
                Print($"Connected!");
                Print("");

                Print("");
                Print("Logging in...");
                // The first thing we must do is login. We only need to so you have an identity.
                // If you don't have an account, just login with the user name and password you want.
                // If the user name doesn't already exist, it will created for you with the given password.
                LoginOptions loginOptions = new LoginOptions()
                {
                    User = new KokkaKoroUser
                    {
                        UserName = "SdkExample",
                        Passcode = "AGoodPasscode"
                    }
                };
                await kokkaKoroService.Login(loginOptions);
                Print("Logged in!");
                Print("");

                // Get a list of the current games.
                Print("");
                Print("Listing current games...");
                List<KokkaKoroGame> games = await kokkaKoroService.ListGames();
                Console.WriteLine($"Current games:");
                foreach (KokkaKoroGame game in games)
                {
                    Console.WriteLine($"  {game.GameName} - Players {game.Players.Count}, {game.Id}");
                }
                Console.WriteLine($"");

                // Get a list of the bots.
                Print($"");
                Print($"Listing hosted bots...");
                List<KokkaKoroBot> bots = await kokkaKoroService.ListBots();
                Console.WriteLine($"Current hosted bots:");
                foreach (KokkaKoroBot bot in bots)
                {
                    Console.WriteLine($"  {bot.Name} - {bot.Major}.{bot.Minor}.{bot.Revision}");
                }
                Print($"");

                Print($"");
                Print($"Creating new game...");
                // Create a new game
                CreateGameOptions options = new CreateGameOptions()
                {
                    GameName = "SDK Example Game"
                };
                KokkaKoroGame newGame = await kokkaKoroService.CreateGame(options);
                Print($"Game Created: {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");
                Print($"");

                Print();
                Print("Adding a hosted bot to the game...");
                // Add bots to the game we just made.
                AddHostedBotOptions botsOpts = new AddHostedBotOptions()
                {
                    BotName = "TestBot",
                    InGameName = "Quinn",
                    GameId = newGame.Id
                };
                newGame = await kokkaKoroService.AddBotToGame(botsOpts);
                Console.WriteLine($"Bot {botsOpts.BotName} added! : {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");
                Print();

                Print();
                Print("Adding another hosted bot to the game...");
                botsOpts = new AddHostedBotOptions()
                {
                    BotName = "TestBot",
                    InGameName = "Marilyn",
                    GameId = newGame.Id
                };
                newGame = await kokkaKoroService.AddBotToGame(botsOpts);
                Console.WriteLine($"Bot {botsOpts.BotName} added! : {newGame.GameName} - Players {newGame.Players.Count}, {newGame.Id}");
                Print();

                // Now start the game.
                Print();
                Print("Starting the game...");
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
                Console.WriteLine("Error!!");
                Console.WriteLine($"   Message: {e.Message}");
                Console.WriteLine($"   Was From Service? {e.IsFromService()}");
            }
            catch (Exception e)
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("Error!");
                Console.WriteLine($"   Message: {e.Message}");
            }

            Print();
            Print();
            Print("Done! Goodbye!");
            Print();
            Print();
        }

        public void Print(string msg = "")
        {
            Console.WriteLine(msg);
        }
    }
}
