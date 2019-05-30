using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ServiceProtocol;
using ServiceProtocol.Common;
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
              
        // Note userName can be null if the name hasn't been set yet. 
        public async Task<KokkaKoroResponse<object>> HandleCommand(string userName, string command)
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
            KokkaKoroResponse<object> result = await InternalHandleCommand(userName, request.Command, command);
            if(result == null)
            {
                result = KokkaKoroResponse<object>.CreateError("Internal Error; response null.");
            }

            // Make sure to set the request id
            result.RequestId = request.RequestId;
            return result;
        }

        private async Task<KokkaKoroResponse<object>> InternalHandleCommand(string userName, KokkaKoroCommands command, string jsonStr)
        {
            // Check if this connection has a user name yet.
            if(String.IsNullOrWhiteSpace(userName))
            {
                if(command != KokkaKoroCommands.SetUserName)
                {
                    return KokkaKoroResponse<object>.CreateError("A user name must be set before anything else.");
                }
            }

            // Handle the command.
            switch (command)
            {
                case KokkaKoroCommands.SetUserName:
                    return SetUserName(jsonStr);
                case KokkaKoroCommands.CreateGame:
                    return CreateGame(jsonStr);
                case KokkaKoroCommands.ListGames:
                    return ListGames(jsonStr);
                case KokkaKoroCommands.ListBots:
                    return await ListBots(jsonStr);
                case KokkaKoroCommands.AddBot:
                    return await AddBot(jsonStr, userName);
                case KokkaKoroCommands.StartGame:
                    return StartGame(jsonStr, userName);
            }
            return KokkaKoroResponse<object>.CreateError("Command not implemented.");
        }

        #region Game Management

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

            // Add it to the dictionary
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

        private KokkaKoroResponse<object> StartGame(string command, string userId)
        {
            // Parse the request options
            KokkaKoroRequest<StartGameOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<StartGameOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse start game", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.GameId.ToString()))
            {
                return KokkaKoroResponse<object>.CreateError("GameId is required.");
            }

            // Find the game
            ServiceGame game = null;
            lock (m_currentGames)
            {
                if (m_currentGames.ContainsKey(request.CommandOptions.GameId))
                {
                    game = m_currentGames[request.CommandOptions.GameId];
                }
            }
            if (game == null)
            {
                return KokkaKoroResponse<object>.CreateError("GameId not found.");
            }

            // Validate the password (if there is one)
            if (!game.ValidatePassword(request.CommandOptions.Password))
            {
                return KokkaKoroResponse<object>.CreateError("Invalid password for gameId.");
            }

            // Try to start it.
            string error = game.StartGame();
            if (String.IsNullOrWhiteSpace(error))
            {
                // Success, return the game info.
                StartGameResponse resp = new StartGameResponse { Game = game.GetInfo() };
                return KokkaKoroResponse<object>.CreateResult(resp);
            }
            else
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to start game: {error}.");
            }
        }

        #endregion

        #region Player Management

        private KokkaKoroResponse<object> SetUserName(string command)
        {
            // Parse the request options
            KokkaKoroRequest<SetUserNameOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<SetUserNameOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse options", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // We ingore the passcode for now.
            if(request.CommandOptions == null || String.IsNullOrWhiteSpace(request.CommandOptions.UserName))
            {
                return KokkaKoroResponse<object>.CreateError("User name must be set in the options.");
            }

            // Set the user name. When we send this message back the websocket will look for it and
            // pull the username out of it.
            SetUserNameResponse response = new SetUserNameResponse()
            {
                AcceptedUserName = request.CommandOptions.UserName,
            };
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        private async Task<KokkaKoroResponse<object>> AddBot(string command, string userId)
        {
            // Parse the request options
            KokkaKoroRequest<AddBotOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<AddBotOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse add bot", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.GameId.ToString()))
            {
                return KokkaKoroResponse<object>.CreateError("GameId is required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.BotName))
            {
                return KokkaKoroResponse<object>.CreateError("BotName is required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.InGameName))
            {
                return KokkaKoroResponse<object>.CreateError("InGameName is required.");
            }

            // Find the game
            ServiceGame game = null;
            lock(m_currentGames)
            {
                if(m_currentGames.ContainsKey(request.CommandOptions.GameId))
                {
                    game = m_currentGames[request.CommandOptions.GameId];
                }
            }
            if(game == null)
            {
                return KokkaKoroResponse<object>.CreateError("GameId not found.");
            }

            // Validate the password (if there is one)
            if(!game.ValidatePassword(request.CommandOptions.Password))
            {
                return KokkaKoroResponse<object>.CreateError("Invalid password for gameId.");
            }
            
            // Try to add the bot.
            string error = await game.AddBot(request.CommandOptions.InGameName, request.CommandOptions.BotName);
            if (String.IsNullOrWhiteSpace(error))
            {
                // Success, return the game info.
                AddBotResponse resp = new AddBotResponse { Game = game.GetInfo() };
                return KokkaKoroResponse<object>.CreateResult(resp);
            }
            else
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to add bot: {error}.");
            }
        }

        #endregion

        #region Bot Managment

        private async Task<KokkaKoroResponse<object>> ListBots(string command)
        {
            ListBotsResponse result = new ListBotsResponse();
            try
            {
                result.Bots = await StorageMaster.Get().ListBots();

                // Null out the passwords
                foreach(KokkaKoroBot info in result.Bots)
                {
                    info.Password = null;
                }

                // Return the results
                return KokkaKoroResponse<object>.CreateResult(result);
            }
            catch (Exception e)
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to list bots: {e.Message}");
            }
        }

        #endregion
    }
}
