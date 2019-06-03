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
using System.Threading;
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

        public GameMaster()
        {
            m_gameManagerThread = new Thread(GameManagerLoop);
            m_gameManagerThread.Start();
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
                if(command != KokkaKoroCommands.Login)
                {
                    return KokkaKoroResponse<object>.CreateError("You must login before calling anything else.");
                }
            }

            // Handle the command.
            switch (command)
            {
                case KokkaKoroCommands.Login:
                    return await Login(jsonStr);
                case KokkaKoroCommands.CreateGame:
                    return CreateGame(jsonStr, userName);
                case KokkaKoroCommands.ListGames:
                    return ListGames(jsonStr);
                case KokkaKoroCommands.ListBots:
                    return await ListBots(jsonStr);
                case KokkaKoroCommands.AddHostedBot:
                    return await AddHostedBot(jsonStr);
                case KokkaKoroCommands.JoinGame:
                    return JoinGame(jsonStr, userName);
                case KokkaKoroCommands.StartGame:
                    return StartGame(jsonStr);
            }
            return KokkaKoroResponse<object>.CreateError("Command not implemented.");
        }

        #region Game Management

        TimeSpan c_maxKeepGameAroundTime = new TimeSpan(30, 0, 0, 0);
        Dictionary<Guid, ServiceGame> m_currentGames = new Dictionary<Guid, ServiceGame>();
        Thread m_gameManagerThread = null;

        private KokkaKoroResponse<object> CreateGame(string command, string userName)
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
            if(String.IsNullOrWhiteSpace(request.CommandOptions.GameName))
            {
                return KokkaKoroResponse<object>.CreateError("GameName is a required option.");
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
                userName,
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

        private KokkaKoroResponse<object> StartGame(string command)
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

            // Try to find and validate the game.
            (ServiceGame game, KokkaKoroResponse<object> error) = GetGame(request.CommandOptions.GameId, request.CommandOptions.Password);
            if (error != null)
            {
                return error;
            }

            // Try to start it.
            string startError = game.StartGame();
            if (String.IsNullOrWhiteSpace(startError))
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

        public void GameManagerLoop()
        {
            while(true)
            {
                DateTime start = DateTime.Now;
                try
                {
                    GameManagerDoWork();
                }
                catch(Exception e)
                {
                    Logger.Error($"Exception in game manager loop.", e);
                }

                // Sleep for a while
                TimeSpan workTime = DateTime.Now - start;
                TimeSpan diff = new TimeSpan(0, 0, 1) - workTime;
                if(diff.TotalMilliseconds > 0)
                {
                    Thread.Sleep(diff);
                }
            }
        }

        public void GameManagerDoWork()
        {
            // First of all, find an remove any old games that exist.
            List<ServiceGame> oldGames = new List<ServiceGame>();
            lock (m_currentGames)
            {
                foreach(KeyValuePair<Guid, ServiceGame> p in m_currentGames)
                {
                    if(DateTime.UtcNow - p.Value.GetCreatedAt() > c_maxKeepGameAroundTime)
                    {
                        oldGames.Add(p.Value);
                    }
                }
                foreach(ServiceGame g in oldGames)
                {
                    m_currentGames.Remove(g.GetId());
                }
            }
            foreach(ServiceGame g in oldGames)
            {
                g.EnsureEnded();
            }

            // Next call the game tick on all active games.
            // But we don't want to hold the map lock, so make a copy of the list
            // and then call on it.
            List<ServiceGame> currentGames = new List<ServiceGame>();
            lock (m_currentGames)
            {
                foreach (KeyValuePair<Guid, ServiceGame> p in m_currentGames)
                {
                    currentGames.Add(p.Value);
                }
            }
            foreach(ServiceGame g in currentGames)
            {
                g.GameTick();
            }
        }

        #endregion

        #region Player Management

        private async Task<KokkaKoroResponse<object>> Login(string command)
        {
            // Parse the request options
            KokkaKoroRequest<LoginOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<LoginOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse options", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if(request.CommandOptions == null 
                || request.CommandOptions.User == null 
                || String.IsNullOrWhiteSpace(request.CommandOptions.User.UserName)
                || String.IsNullOrWhiteSpace(request.CommandOptions.User.Passcode))
            {
                return KokkaKoroResponse<object>.CreateError("User name and passcode must be sent in command options.");
            }
            if(request.CommandOptions.User.Passcode.Length < 5)
            {
                return KokkaKoroResponse<object>.CreateError("The passcode must be longer than 4 chars.");
            }

            // Check to make sure the user and passcode are correct.
            try
            {
                if (!await UserMaster.Get().ValidateUserPasscode(request.CommandOptions.User))
                {
                    return KokkaKoroResponse<object>.CreateError("Invalid passcode for the given user.");
                }
            }
            catch(Exception e)
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to validate user: {e.Message}");
            }

            // Set the user name. When we send this message back the websocket will look for it and
            // pull the username out of it.
            LoginResponse response = new LoginResponse()
            {
                UserName = request.CommandOptions.User.UserName,
            };
            return KokkaKoroResponse<object>.CreateResult(response);
       }

        private async Task<KokkaKoroResponse<object>> AddHostedBot(string command)
        {
            // Parse the request options
            KokkaKoroRequest<AddHostedBotOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<AddHostedBotOptions>>(command);
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

            // Try to find and validate the game.
            (ServiceGame game, KokkaKoroResponse<object> error) = GetGame(request.CommandOptions.GameId, request.CommandOptions.Password);
            if (error != null)
            {
                return error;
            }

            // Try to add the bot.
            return await game.AddHostedBot(request.CommandOptions.InGameName, request.CommandOptions.BotName);
        }

        private KokkaKoroResponse<object> JoinGame(string command, string userName)
        {
            // Parse the request options
            KokkaKoroRequest<JoinGameOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<JoinGameOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse join game", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (request.CommandOptions.GameId.Equals(Guid.Empty))
            {
                return KokkaKoroResponse<object>.CreateError("GameId is required.");
            }

            // Try to find and validate the game.
            (ServiceGame game, KokkaKoroResponse<object> error) = GetGame(request.CommandOptions.GameId, request.CommandOptions.Password);
            if(error != null)
            {
                return error;
            }

            // Try to join the game!
            return game.JoinGame(userName);
        }

        private (ServiceGame, KokkaKoroResponse<object>) GetGame(Guid gameId, string password)
        {
            // Find the game
            ServiceGame game = null;
            lock (m_currentGames)
            {
                if (m_currentGames.ContainsKey(gameId))
                {
                    game = m_currentGames[gameId];
                }
            }
            if (game == null)
            {
                return (null, KokkaKoroResponse<object>.CreateError("GameId not found."));
            }

            // Validate the password (if there is one)
            if (!game.ValidatePassword(password))
            {
                return (null, KokkaKoroResponse<object>.CreateError("The password is incorrect for the game."));
            }
            return (game, null);
        }

        #endregion

        #region Bot Management

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
