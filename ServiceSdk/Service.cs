using GameCommon.Protocol;
using Newtonsoft.Json;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using ServiceSdk;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace KokkaKoro
{
    public delegate Task DisconnectCallback(bool isClientInvoked);
    public delegate Task GameUpdatesCallback(List<GameLog> newLogItems);

    internal interface IWebSocketHandler
    {
        Task OnGameUpdates(string jsonStr);

        Task OnDisconnect(bool isFromClient);
    }

    public class Service : IWebSocketHandler, ILogger
    {
        // A callback that fires whenever there are any game the user is joined or spectating.
        // This is how you get game update notifications and notifications when it's your turn.
        public event GameUpdatesCallback OnGameUpdates;

        // A callback that fires when we get disconnected from the service.
        public event DisconnectCallback OnDisconnected;

        KokkaKoroClientWebsocket m_websocket;
        ILogger m_logger;
        bool m_debugWebsocketMessages = false;

        // Turns on SDK debugging.
        public void SetDebugging(ILogger logger, bool showWebsocketMessages = false)
        {
            m_logger = logger;
            m_debugWebsocketMessages = showWebsocketMessages;
            if(m_websocket != null)
            {
                m_websocket.SetLogMessages(showWebsocketMessages);
            }
        }

        // Connects to the service. 
        // If local port is given, we will try to connect to the localhost port.
        public async Task ConnectAsync(string connectionAddress = null)
        {
            m_websocket = new KokkaKoroClientWebsocket(this, this, m_debugWebsocketMessages);
            string url = $"{(connectionAddress == null ? "wss://kokkakoro.azurewebsites.net" : connectionAddress)}/ws";
            Info($"Connecting to {url}");
            await m_websocket.Connect(url);
        }

        // Disconnects the SDK from the service.
        public async Task Disconnect()
        {
           await m_websocket.Disconnect();
        }

        // Fires when the websocket receives a game update message.
        async Task IWebSocketHandler.OnGameUpdates(string jsonStr)
        {
            KokkaKoroResponse<List<GameLog>> broadcastMsg = null;
            try
            {
                broadcastMsg = JsonConvert.DeserializeObject<KokkaKoroResponse<List<GameLog>>>(jsonStr);
            }
            catch (Exception e)
            {
                Error($"Failed to parse GameUpdate message.", e);
                return;
            }

            if (broadcastMsg == null || broadcastMsg.Data == null)
            {
                Error($"No data in broadcast message");
                return;
            }

            // On game updates, pass them to the consumer.
            try
            {
                await OnGameUpdates?.Invoke(broadcastMsg.Data);
            }
            catch(Exception e)
            {
                Error($"OnGameUpdates function had an unhanded exception.", e);
            }
        }

        // Fires when the websocket closes.
        async Task IWebSocketHandler.OnDisconnect(bool isFromClient)
        {
            try
            {
                await OnDisconnected?.Invoke(isFromClient);
            }
            catch (Exception e)
            {
                Error($"OnDisconnected function had an unhanded exception.", e);
            }
        }

        private async Task<KokkaKoroResponse<T>> MakeRequest<T>(KokkaKoroRequest<object> request, string requestName, bool expectData = true)
        {
            // Call and get a response.
            string response = await m_websocket.SendRequest(request);
            if (String.IsNullOrWhiteSpace(response))
            {
                Error($"${requestName} request failed.");
                throw new KokkaKoroException("Server returned an empty mes.", false);
            }

            // Parse the response.
            KokkaKoroResponse<T> obj = JsonConvert.DeserializeObject<KokkaKoroResponse<T>>(response);
            if (!String.IsNullOrWhiteSpace(obj.Error))
            {
                Error($"Service failed to ${requestName}: {obj.Error}");
                throw new KokkaKoroException(obj.Error, true);
            }

            // Validate we got a data object.
            if (expectData && obj.Data == null)
            {
                Error($"${requestName} failed to get data object in response.");
                throw new KokkaKoroException("Failed to find data object in response.", true);
            }
            return obj;
        }

        #region User Stuff

        public async Task Login(LoginOptions options)
        {
            if (options == null || options.User == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.User.UserName))
            {
                throw new KokkaKoroException("A user name is required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.User.Passcode))
            {
                throw new KokkaKoroException("A passcode is required!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.Login,
                CommandOptions = options
            };

            // Make the request.
            await MakeRequest<LoginResponse>(request, "set user name");
        }

        #endregion

        #region Game Stuff

        public async Task<KokkaKoroGame> CreateGame(CreateGameOptions options)
        {
            if(options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if(String.IsNullOrWhiteSpace(options.GameName))
            {
                throw new KokkaKoroException("GameName is required!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.CreateGame,
                CommandOptions = options
            };

            KokkaKoroResponse<CreateGameResponse> response = await MakeRequest<CreateGameResponse>(request, "create game");

            return response.Data.Game;
        }

        public async Task<List<KokkaKoroGame>> ListGames()
        {
            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.ListGames
            };

            // Make the request and validate.
            KokkaKoroResponse<ListGamesResponse> response = await MakeRequest<ListGamesResponse>(request, "list game");

            // Pull out the list to return.
            List<KokkaKoroGame> games = new List<KokkaKoroGame>();
            foreach(KokkaKoroGame game in response.Data.Games)
            {
                games.Add(game);
            }
            return games;
        }

        public async Task<GetGameLogsResponse> GetGameLogs(GetGameLogsOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (options.GameId.Equals(Guid.Empty))
            {
                throw new KokkaKoroException("A GameId is required.!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.GetGameLogs,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<GetGameLogsResponse> response = await MakeRequest<GetGameLogsResponse>(request, "get game logs");
            return response.Data;
        }

        public async Task<KokkaKoroGame> JoinGame(JoinGameOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if(options.GameId.Equals(Guid.Empty))
            {
                throw new KokkaKoroException("A GameId is required.!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.JoinGame,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<JoinGameResponse> response = await MakeRequest<JoinGameResponse>(request, "join game");

            // Pull out the list to return.
            return response.Data.Game;
        }

        public async Task<KokkaKoroGame> StartGame(StartGameOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (options.GameId.Equals(Guid.Empty))
            {
                throw new KokkaKoroException("A GameId is required.!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.StartGame,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<StartGameResponse> response = await MakeRequest<StartGameResponse>(request, "start game");

            // Pull out the list to return.
            return response.Data.Game;
        }

        public async Task<SendGameActionResponse> SendGameAction(SendGameActionOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (options.GameId.Equals(Guid.Empty))
            {
                throw new KokkaKoroException("A GameId is required.!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.SendGameAction,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<SendGameActionResponse> response = await MakeRequest<SendGameActionResponse>(request, "send game action");

            // Pull out the list to return.
            return response.Data;
        }

        #endregion

        #region Bot Stuff

        public async Task<List<KokkaKoroBot>> ListBots()
        {
            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.ListBots
            };

            // Make the request and validate.
            KokkaKoroResponse<ListBotsResponse> response = await MakeRequest<ListBotsResponse>(request, "list bots");

            // Pull out the list to return.
            List<KokkaKoroBot> bots = new List<KokkaKoroBot>();
            foreach (KokkaKoroBot bot in response.Data.Bots)
            {
                bots.Add(bot);
            }
            return bots;
        }

        public async Task<KokkaKoroGame> AddBotToGame(AddHostedBotOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.BotName))
            {
                throw new KokkaKoroException("BotName is required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.InGameName))
            {
                throw new KokkaKoroException("InGameName is required!", false);
            }
            if (options.GameId.Equals(Guid.Empty))
            {
                throw new KokkaKoroException("A GameId is required.!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.AddHostedBot,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<AddHostedBotResponse> response = await MakeRequest<AddHostedBotResponse>(request, "add to game bot");

            // Pull out the list to return.
            return response.Data.Game;
        }

        public async Task<KokkaKoroBot> AddOrUploadBot(AddOrUpdateBotOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (!options.Bot.IsValid())
            {
                throw new KokkaKoroException("The bot details are not valid!", false);
            }
            if (String.IsNullOrWhiteSpace(options.Base64EncodedZipedBotFiles))
            {
                throw new KokkaKoroException("The bot files are required!", false);
            }
            
            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.AddOrUpdateBot,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<AddOrUpdateBotResponse> response = await MakeRequest<AddOrUpdateBotResponse>(request, "add or upload bot");

            // Return the new bot.
            return response.Data.Bot;
        }

        #endregion

        #region Tournament Stuff

        public async Task<KokkaKoroTournament> CreateTournament(CreateTournamentOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.Name))
            {
                throw new KokkaKoroException("A name is required!", false);
            }
            if (options.BotsPerGame < 2 || options.BotsPerGame > 4)
            {
                throw new KokkaKoroException("The bot count must be between 2-4!", false);
            }
            if(options.NumberOfGames < 1 || options.NumberOfGames > 1000)
            {
                throw new KokkaKoroException("The bot count must be between 1-1000!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.CreateTournament,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<CreateTournamentResponse> response = await MakeRequest<CreateTournamentResponse>(request, "create tournament");
            return response.Data.Tournament;
        }

        public async Task<List<KokkaKoroTournament>> ListTournaments()
        {
            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.ListTournaments
            };

            // Make the request and validate.
            KokkaKoroResponse<ListTournamentResponse> response = await MakeRequest<ListTournamentResponse>(request, "list tournaments");
            return response.Data.Tournaments;
        }

        #endregion

        public void Info(string msg)
        {
            ILogger l = m_logger;
            if(l != null)
            {
                l.Info(msg);
            }
        }

        public void Warn(string msg)
        {
            ILogger l = m_logger;
            if (l != null)
            {
                l.Warn(msg);
            }
        }

        public void Error(string msg, Exception e = null)
        {
            ILogger l = m_logger;
            if (l != null)
            {
                l.Error(msg, e);
            }
        }

        public void Fatial(string msg, Exception e = null)
        {
            ILogger l = m_logger;
            if (l != null)
            {
                l.Fatial(msg, e);
            }
        }
    }
}
