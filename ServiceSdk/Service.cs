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
    public class Service
    {
        KokkaKoroClientWebsocket m_websocket;

        public Service()
        {
        }

        public void SetDebugging(bool state)
        {
            Logger.SetDebug(state);
        }

        public async Task ConnectAsync(int? localPort = null)
        {
            m_websocket = new KokkaKoroClientWebsocket();
            string url = localPort.HasValue ? $"ws://localhost:{localPort.Value}/ws" : $"wss://kokkakoro.azurewebsites.net/ws";
            Logger.Info($"Connecting to {url}");
            await m_websocket.Connect(url);
        }

        private async Task<KokkaKoroResponse<T>> MakeRequest<T>(KokkaKoroRequest<object> request, string requestName, bool expectData = true)
        {
            // Call and get a response.
            string response = await m_websocket.SendRequest(request);
            if (String.IsNullOrWhiteSpace(response))
            {
                Logger.Error($"${requestName} request failed.");
                throw new KokkaKoroException("Server returned an empty mes.", false);
            }

            // Parse the response.
            KokkaKoroResponse<T> obj = JsonConvert.DeserializeObject<KokkaKoroResponse<T>>(response);
            if (!String.IsNullOrWhiteSpace(obj.Error))
            {
                Logger.Error($"Service failed to ${requestName}: {obj.Error}");
                throw new KokkaKoroException(obj.Error, true);
            }

            // Validate we got a data object.
            if (expectData && obj.Data == null)
            {
                Logger.Error($"${requestName} failed to get data object in response.");
                throw new KokkaKoroException("Failed to find data object in response.", true);
            }
            return obj;
        }

        #region Game Stuff

        public async Task<KokkaKoroGame> CreateGame(CreateGameOptions options)
        {
            if(options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if(String.IsNullOrWhiteSpace(options.GameName) || String.IsNullOrWhiteSpace(options.CreatedBy))
            {
                throw new KokkaKoroException("GameName and CreatedBy are required!", false);
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

        public async Task<KokkaKoroGame> AddBotToGame(AddBotOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
            }
            if (String.IsNullOrWhiteSpace(options.BotName))
            {
                throw new KokkaKoroException("BotName is required!", false);
            }

            // Build the request
            KokkaKoroRequest<object> request = new KokkaKoroRequest<object>()
            {
                Command = KokkaKoroCommands.AddBot,
                CommandOptions = options
            };

            // Make the request and validate.
            KokkaKoroResponse<AddBotResponse> response = await MakeRequest<AddBotResponse>(request, "add bot");

            // Pull out the list to return.
            return response.Data.Game;
        }

        public async Task<KokkaKoroGame> StartGame(StartGameOptions options)
        {
            if (options == null)
            {
                throw new KokkaKoroException("Options are required!", false);
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

        #endregion
    }
}
