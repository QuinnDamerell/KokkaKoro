using GameCommon;
using GameService.Managers;
using Microsoft.CodeAnalysis.Operations;
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
                    return CreateGameInternal(jsonStr, userName);
                case KokkaKoroCommands.ListGames:
                    return ListGames();
                case KokkaKoroCommands.ListBots:
                    return await ListBots(jsonStr);
                case KokkaKoroCommands.AddHostedBot:
                    return await AddHostedBot(jsonStr);
                case KokkaKoroCommands.JoinGame:
                    return JoinGame(jsonStr, userName);
                case KokkaKoroCommands.StartGame:
                    return StartGame(jsonStr);
                case KokkaKoroCommands.SendGameAction:
                    return SendGameAction(jsonStr, userName);
                case KokkaKoroCommands.GetGameLogs:
                    return GetGameLogs(jsonStr, userName);
                case KokkaKoroCommands.AddOrUpdateBot:
                    return await AddOrUploadBot(jsonStr, userName);
                case KokkaKoroCommands.CreateTournament:
                    return await TournamentMaster.Get().Create(jsonStr, userName);
                case KokkaKoroCommands.ListTournaments:
                    return TournamentMaster.Get().List(jsonStr, userName);
                case KokkaKoroCommands.Heartbeat:
                    return KokkaKoroResponse<object>.CreateResult(null);
            }
            return KokkaKoroResponse<object>.CreateError("Command not implemented.");
        }

        #region Game Management

        TimeSpan c_maxKeepGameAroundTime = new TimeSpan(30, 0, 0, 0);
        Dictionary<Guid, ServiceGame> m_currentGames = new Dictionary<Guid, ServiceGame>();
        Thread m_gameManagerThread = null;

        public void GameManagerLoop()
        {
            while (true)
            {
                DateTime start = DateTime.Now;
                try
                {
                    GameManagerDoWork();
                }
                catch (Exception e)
                {
                    Logger.Error($"Exception in game manager loop.", e);
                }

                // Sleep for a while
                TimeSpan workTime = DateTime.Now - start;
                TimeSpan diff = new TimeSpan(0, 0, 1) - workTime;
                if (diff.TotalMilliseconds > 0)
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
                foreach (KeyValuePair<Guid, ServiceGame> p in m_currentGames)
                {
                    if (DateTime.UtcNow - p.Value.GetCreatedAt() > c_maxKeepGameAroundTime)
                    {
                        oldGames.Add(p.Value);
                    }
                }
                foreach (ServiceGame g in oldGames)
                {
                    m_currentGames.Remove(g.GetId());
                }
            }
            foreach (ServiceGame g in oldGames)
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
            foreach (ServiceGame g in currentGames)
            {
                g.GameTick();
            }
        }

        private KokkaKoroResponse<object> CreateGameInternal(string command, string userName)
        {
            KokkaKoroRequest<CreateGameOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<CreateGameOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse create game", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.GameName))
            {
                return KokkaKoroResponse<object>.CreateError("GameName is a required option.");
            }
            TimeSpan? gameTime = null;
            if (request.CommandOptions.GameTimeLimitSeconds.HasValue)
            {
                gameTime = new TimeSpan(0, 0, request.CommandOptions.GameTimeLimitSeconds.Value);
            }          

            // Create the game
            ServiceGame game = CreateGame(request.CommandOptions.GameName, request.CommandOptions.Password, gameTime, userName);

            // Return the game info.
            CreateGameResponse response = new CreateGameResponse { Game = game.GetInfo() };
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        public ServiceGame CreateGame(string gameName, string password, TimeSpan? gameTimeLimit, string userName)
        { 
            // Create the game
            ServiceGame game = new ServiceGame(
                gameName,
                userName,
                password,
                gameTimeLimit
                );

            // Add it to the dictionary
            lock(m_currentGames)
            {
                m_currentGames.Add(game.GetId(), game);
            }

            return game;    
        }

        public KokkaKoroResponse<object> ListGames()
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
            if (request.CommandOptions.GameId.Equals(Guid.Empty))
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

        private KokkaKoroResponse<object> GetGameLogs(string command, string userName)
        {
            // Parse the request options
            KokkaKoroRequest<GetGameLogsOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<GetGameLogsOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse get game options", e);
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
            (ServiceGame game, KokkaKoroResponse<object> error) = GetGame(request.CommandOptions.GameId, null, true);
            if (error != null)
            {
                return error;
            }

            // Try to get the logs.
            GetGameLogsResponse response = game.GetGameLogs();
            if (response != null)
            {              
                return KokkaKoroResponse<object>.CreateResult(response);
            }
            else
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to get game logs.");
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

            // Check that the protocol and game version match, otherwise deny the client.
            if (request.CommandOptions.GameVersion != GameState.GameVersion 
                || request.CommandOptions.ProtocolVersion != KokkaKoroRequest<object>.ProtocolVersion)
            {
                return KokkaKoroResponse<object>.CreateError($"The protocol or game version of this client is too old. Please update it. [You sent p:{request.CommandOptions.ProtocolVersion} g:{request.CommandOptions.GameVersion}, server p:{KokkaKoroRequest<object>.ProtocolVersion} g:{GameState.GameVersion}]");
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

        private KokkaKoroResponse<object> SendGameAction(string command, string userName)
        {
            // Parse the request options
            KokkaKoroRequest<SendGameActionOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<SendGameActionOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse game action", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (request.CommandOptions.Action == null)
            {
                return KokkaKoroResponse<object>.CreateError("An action in the options is required.");
            }
            if (request.CommandOptions.GameId.Equals(Guid.Empty))
            {
                return KokkaKoroResponse<object>.CreateError("GameId is required.");
            }

            // Try to find and validate the game.
            (ServiceGame game, KokkaKoroResponse<object> error) = GetGame(request.CommandOptions.GameId, null, true);
            if (error != null)
            {
                return error;
            }

            // Try to start it.
            SendGameActionResponse response = game.SendGameAction(request.CommandOptions.Action, userName);
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        private (ServiceGame, KokkaKoroResponse<object>) GetGame(Guid gameId, string password, bool bypassPassword = false)
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
            if (!bypassPassword)
            {
                if (!game.ValidatePassword(password))
                {
                    return (null, KokkaKoroResponse<object>.CreateError("The password is incorrect for the game."));
                }
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

        private async Task<KokkaKoroResponse<object>> AddOrUploadBot(string command, string userName)
        {
            KokkaKoroRequest<AddOrUpdateBotOptions> request;
            try
            {
                request = JsonConvert.DeserializeObject<KokkaKoroRequest<AddOrUpdateBotOptions>>(command);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse add or update bot", e);
                return KokkaKoroResponse<object>.CreateError("Failed to parse command options.");
            }

            // Validate
            if (request.CommandOptions == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (request.CommandOptions.Bot == null)
            {
                return KokkaKoroResponse<object>.CreateError("Command options are required.");
            }
            if (String.IsNullOrWhiteSpace(request.CommandOptions.Base64EncodedZipedBotFiles))
            {
                return KokkaKoroResponse<object>.CreateError("The bot files are required.");
            }

            // Do work.
            try
            {
                KokkaKoroBot bot = await BotManager.Get().UploadBot(request.CommandOptions, userName);
                return KokkaKoroResponse<object>.CreateResult(new AddOrUpdateBotResponse() { Bot = bot });
            }
            catch(Exception e)
            {
                return KokkaKoroResponse<object>.CreateError($"Upload new bot: {e.Message}");
            }
        }        

        #endregion
    }
}
