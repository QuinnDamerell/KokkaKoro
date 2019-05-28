using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceProtocol;
using ServiceProtocol.Requests;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class GameMaster
    {
        private static GameMaster s_instance = new GameMaster();
        public static GameMaster Get()
        {
            return s_instance;
        }

        public KokkaKoroResponse<object> HandleCommand(string userId, string command)
        {
            if(String.IsNullOrWhiteSpace(command))
            {
                return KokkaKoroResponse<object>.CreateError("No command.");
            }

            // Decode the string is needed.
            command = WebUtility.UrlDecode(command);

            // Parse out the command type
            KokkaKoroRequest<object> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<object>>(command);
            }
            catch(Exception e)
            {
                Logger.Error("Failed to parse command", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command.");
            }

            // Handle the command
            KokkaKoroResponse<object> result = InternalHandleCommand(userId, request.Command, command);
            if(result == null)
            {
                result = KokkaKoroResponse<object>.CreateError("Internal Error; response null.");
            }

            // Make sure to set the request id
            result.RequestId = request.RequestId;
            return result;
        }

        private KokkaKoroResponse<object> InternalHandleCommand(string userId, KokkaKoroCommands command, string jsonStr)
        {
            // Handle the command.
            switch (command)
            {
                case KokkaKoroCommands.CreateGame:
                    return CreateGame(jsonStr);
                case KokkaKoroCommands.ListGames:
                    return ListGames(jsonStr);
            }
            return KokkaKoroResponse<object>.CreateError("Command not implmented.");
        }

        #region Game Managment

        Dictionary<Guid, ServiceGame> m_currentGames = new Dictionary<Guid, ServiceGame>();

        private KokkaKoroResponse<object> CreateGame(string command)
        {
            KokkaKoroRequest<CreateGameOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<CreateGameOptions>>(command);
            }
            catch(Exception e)
            {
                Logger.Error("Failed to parse create game", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            if(request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }

            // Validate
            if (request.CommandOptions.PlayerLimit.HasValue && request.CommandOptions.PlayerLimit.Value < 0)
            {
                return KokkaKoroResponse<object>.CreateError("Invalid player limit.");
            }
            if(String.IsNullOrWhiteSpace(request.CommandOptions.GameName) || String.IsNullOrWhiteSpace(request.CommandOptions.CreatedBy))
            {
                return KokkaKoroResponse<object>.CreateError("GameName and CreatedBy are required options.");
            }

            TimeSpan? turnTime = null;
            if(request.CommandOptions.TurnTimeLmitSeconds.HasValue)
            {
                turnTime = new TimeSpan(0, 0, request.CommandOptions.TurnTimeLmitSeconds.Value);
            }
            TimeSpan? minTurn = null;
            if (request.CommandOptions.MinTurnTimeSeconds.HasValue)
            {
                minTurn = new TimeSpan(0, 0, request.CommandOptions.MinTurnTimeSeconds.Value);
            }
            TimeSpan? gameTime = null;
            if (request.CommandOptions.GameTimeLimitSeconds.HasValue)
            {
                gameTime = new TimeSpan(0, 0, request.CommandOptions.GameTimeLimitSeconds.Value);
            }

            // Create the game
            ServiceGame game = new ServiceGame(
                request.CommandOptions.PlayerLimit,
                request.CommandOptions.GameName,
                request.CommandOptions.CreatedBy,
                request.CommandOptions.Password,
                turnTime,
                minTurn,
                gameTime
                );

            // Add it to the dictonary
            lock(m_currentGames)
            {
                m_currentGames.Add(game.GetId(), game);
            }

            // Return the game info.
            CreateGameResponse response = new CreateGameResponse { Game = game.GetInfo() };
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        private KokkaKoroResponse<object> ListGames(string command)
        {
            ListGamesResponse result = new ListGamesResponse();
            result.Games = new List<ServiceProtocol.Common.KokkaKoroGame>();
            lock(m_currentGames)
            {
                foreach(KeyValuePair<Guid, ServiceGame> game in m_currentGames)
                {
                    result.Games.Add(game.Value.GetInfo());
                }
            }
            return KokkaKoroResponse<object>.CreateResult(result);
        }

        #endregion
    }
}
