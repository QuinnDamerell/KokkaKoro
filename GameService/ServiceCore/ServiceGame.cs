using GameCore;
using GameCommon.Protocol;
using GameService.WebsocketsHelpers;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameCommon;
using GameService.Managers;

namespace GameService.ServiceCore
{
    public class ServiceGame
    {
        static readonly TimeSpan c_maxGameLength = new TimeSpan(0, 10, 0);
        static readonly int c_maxRoundCount      = 5000;
        static readonly int c_maxPlayerCount     = 4;

        Guid m_id;
        KokkaKoroGameState m_state;
        object m_gameLock = new object();
        string m_password;
        string m_gameName;
        string m_createdBy;
        int m_roundLimit;
        TimeSpan m_gameTimeLmit;
        DateTime m_createdAt;
        DateTime? m_startedAt;
        DateTime? m_gameStartedAt;
        DateTime? m_gameEndedAt;
        string m_fatalError = null;

        // The current players.
        List<ServicePlayer> m_players = new List<ServicePlayer>();

        // Game stuff
        GameEngine m_gameEngine;

        public ServiceGame(string gameName = null, string createdBy = null, string password = null, TimeSpan? gameTimeLimit = null, int? roundLimit = null)
        {
            m_state = KokkaKoroGameState.Lobby;
            m_id = Guid.NewGuid();
            m_createdAt = DateTime.UtcNow;
            m_roundLimit = roundLimit.HasValue ? roundLimit.Value : int.MaxValue;
            m_gameTimeLmit = gameTimeLimit.HasValue ? gameTimeLimit.Value : TimeSpan.MaxValue;
            m_gameName = String.IsNullOrWhiteSpace(gameName) ? $"game-{m_id}" : gameName;
            m_createdBy = String.IsNullOrWhiteSpace(gameName) ? $"unknown" : createdBy;
            m_password = String.IsNullOrWhiteSpace(password) ? null : password;

            // Limit the game length.
            if(m_gameTimeLmit > c_maxGameLength)
            {
                m_gameTimeLmit = c_maxGameLength;
            }
            if(m_roundLimit > c_maxRoundCount)
            {
                m_roundLimit = c_maxRoundCount;
            }
        }

        public Guid GetId()
        {
            return m_id;
        }

        public DateTime GetCreatedAt()
        {
            return m_createdAt;
        }

        public bool ValidatePassword(string userPassword)
        {
            if(String.IsNullOrWhiteSpace(m_password))
            {
                return true;
            }
            if(String.IsNullOrWhiteSpace(userPassword))
            {
                return false;
            }
            return m_password.Equals(userPassword);
        }

        #region Setup Stuff

        public async Task<KokkaKoroResponse<object>> AddHostedBot(string inGameName, string botName)
        {
            if(String.IsNullOrWhiteSpace(inGameName) || String.IsNullOrWhiteSpace(botName))
            {
                return KokkaKoroResponse<object>.CreateError("No bot name or in game name specified.");
            }

            // Do a quick state check.
            lock(m_gameLock)
            {
                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game not in joinable state");
                }               
                if (m_players.Count() >= c_maxPlayerCount)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }
            }

            // Download the bot locally.
            ServiceBot bot;
            try
            {
                bot = await BotManager.Get().GetBotCopy(botName);
            }
            catch(Exception e)
            {
                return KokkaKoroResponse<object>.CreateError($"Failed to load bot: {e.Message}");
            }     
            
            lock(m_gameLock)
            {
                // Check the state again.
                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game not in joinable state");
                }
                if (m_players.Count() >= c_maxPlayerCount)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }

                // Make sure the bot name is unique
                GetUniqueName(ref inGameName);

                // Add the player.
                m_players.Add(new ServicePlayer(bot, inGameName));                
            }

            // Create a response
            AddHostedBotResponse response = new AddHostedBotResponse()
            {
                Game = GetInfo(),
                Bot = bot.GetBotInfo(),
                WasInCache = bot.WasInCache(),
            };      
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        public KokkaKoroResponse<object> JoinGame(string userName)
        {
            if (String.IsNullOrWhiteSpace(userName))
            {
                return KokkaKoroResponse<object>.CreateError("You must have a user name to join a game.");
            }

            ServicePlayer foundUser = null;
            lock (m_gameLock)
            {
                // Search to see if the user name already exists.
                foreach(ServicePlayer p in m_players)
                {
                    if(p.GetUserName().Equals(userName))
                    {
                        foundUser = p;
                        break;
                    }
                }

                // Make sure the user we found is a bot that's connecting, if not deny them.
                if(foundUser != null && !foundUser.IsBot())
                {
                    return KokkaKoroResponse<object>.CreateError($"This user has already joined the game.");
                }

                // Check the state again.
                if ((foundUser == null && m_state != KokkaKoroGameState.Lobby) || (foundUser != null && m_state != KokkaKoroGameState.WaitingForHostedBots))
                {
                    return KokkaKoroResponse<object>.CreateError($"Game not in joinable state");
                }

                // If this isn't a bot connecting to the game, the remote player.
                if (foundUser == null)
                {
                    // Check the player count.
                    if (m_players.Count() >= c_maxPlayerCount)
                    {
                        return KokkaKoroResponse<object>.CreateError($"Game full");
                    }

                    // Add the player.
                    m_players.Add(new ServicePlayer(userName));
                }
            }

            // Outside of the lock, if this was a hosted bot connecting tell it now.
            if(foundUser != null)
            {
                foundUser.SetBotJoined();

                // Check if all of the player are ready now.
                lock(m_gameLock)
                {
                    bool allReady = true;
                    foreach(ServicePlayer p in m_players)
                    {
                        if(!p.IsReady())
                        {
                            allReady = false;
                            break;
                        }
                    }

                    if(allReady)
                    {
                        StartGameInternal();
                    }
                }
            }

            // Create a response
            JoinGameResponse response = new JoinGameResponse()
            {
                Game = GetInfo()
            };
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        private void GetUniqueName(ref string userName)
        {
            int count = 0;
            while (count < m_players.Count())
            {
                // See if this name matches any other names.
                if (m_players[count].GetInGameName().ToLower() == userName.ToLower())
                {
                    // Append an ending
                    Random r = new Random();
                    switch (r.Next(0, 5))
                    {
                        case 0:
                            userName += "est";
                            break;
                        case 1:
                            userName += "er";
                            break;
                        case 2:
                            userName = "Dr " + userName;
                            break;
                        case 3:
                            userName += " the second";
                            break;
                        case 4:
                            userName += " the great";
                            break;
                    }

                    // Restart the search.
                    count = 0;
                }
                count++;
            }
        }

        private void EnsureUniqueNames()
        {
            int inner = 0;
            int outer = 0;
            while (inner < m_players.Count())
            {
                while(outer < m_players.Count())
                {
                    if(inner != outer && m_players[inner].GetInGameName() == m_players[outer].GetInGameName())
                    {
                        // If we have the same name, try to rename one.                        
                        if(m_players[inner].IsBot())
                        {
                            string name = m_players[inner].GetInGameName();
                            GetUniqueName(ref name);
                            m_players[inner].SetInGameName(name);
                        }
                        else if (m_players[outer].IsBot())
                        {
                            string name = m_players[outer].GetInGameName();
                            GetUniqueName(ref name);
                            m_players[outer].SetInGameName(name);
                        }
                    }
                    outer++;
                }
                inner++;
                outer = 0;
            }
        }

        public string StartGame()
        {
            lock (m_gameLock)
            {
                if (m_players.Count < 2)
                {
                    return "There must be at least two players to start the game.";
                }

                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return "Invalid state to start game";
                }

                // Put the game in the waiting for bots state.
                m_state = KokkaKoroGameState.WaitingForHostedBots;
            }

            // Make sure all player names are unique.
            // Note this can change the name of a bot that has already been created,
            // but who cares.
            EnsureUniqueNames();

            // Note the time.
            m_startedAt = DateTime.UtcNow;

            // Spawn the any bot players.
            bool hasBots = false;
            foreach (ServicePlayer p in m_players)
            {
                if (p.IsBot())
                {
                    hasBots = true;
                    p.StartBot(GetId(), m_password);
                }
            }

            if(!hasBots)
            {
                StartGameInternal();
            }

            return null;
        }

        private void StartGameInternal()
        {
            lock (m_gameLock)
            {
                if (m_state != KokkaKoroGameState.Lobby && m_state != KokkaKoroGameState.WaitingForHostedBots)
                {
                    return;
                }
                m_state = KokkaKoroGameState.InProgress;
            }

            // First of all, take all of the players and shuffle them to get the player order.
            Random random = new Random((int)DateTime.Now.Ticks);
            for (int i = 0; i < m_players.Count * 10; i++)
            {
                int from = random.Next(0, m_players.Count);
                int to = random.Next(0, m_players.Count);
                ServicePlayer tmp = m_players[to];
                m_players[to] = m_players[from];
                m_players[from] = tmp;
            }

            // Now let's begin!
            m_gameStartedAt = DateTime.UtcNow;
            List<InitalPlayer> players = new List<InitalPlayer>();
            foreach (ServicePlayer p in m_players)
            {
                players.Add(new InitalPlayer() { FriendlyName = p.GetInGameName(), UserName = p.GetUserName() });
            }
            m_gameEngine = new GameEngine(players, GameMode.Base, m_roundLimit);

            // Invoke the first null action to get the ball rolling.
            // Ignore the response.
            (GameActionResponse response, List<GameLog> actionLog) = m_gameEngine.ConsumeAction(null, m_players[0].GetUserName());

            // Send the messages generated by the action to the players.
            BroadcastMessages(actionLog);
        }

        #endregion

        public GetGameLogsResponse GetGameLogs()
        {
            GetGameLogsResponse response = new GetGameLogsResponse();
            response.BotLogs = new List<KokkaKoroBotLog>();
            response.Game = GetInfo();
            if(m_gameEngine != null)
            {
                response.GameLog = m_gameEngine.GetLogs();
                response.State = m_gameEngine.GetState();
            }

            // Get a copy of the current players.
            List<ServicePlayer> players = new List<ServicePlayer>();
            lock (m_gameLock)
            {
                foreach (ServicePlayer p in m_players)
                {
                    players.Add(p);
                }
            }

            // Outside of lock, get the logs.
            foreach(ServicePlayer p in players)
            {
                if (p.IsBot())
                {
                    KokkaKoroBotLog log = new KokkaKoroBotLog();
                    log.Player = p.GetInfo();
                    log.Bot = p.GetBotInfo();
                    log.StdOut = p.GetBotStdOut();
                    log.StdErr = p.GetBotStdErr();
                    response.BotLogs.Add(log);
                }
            }

            return response;
        }

        public SendGameActionResponse SendGameAction(GameAction<object> action, string userName)
        {
            lock (m_gameLock)
            {
                // Make sure the game is active.
                if (m_state != KokkaKoroGameState.InProgress)
                {
                    return SendGameActionResponse.CreateResponse(GameActionResponse.CreateError(GameError.Create(null, ErrorTypes.Unknown, "Game isn't in progress", false)));
                }
            }

            // Send the game action 
            // Note, this will validate if the user is in the game for us.
            (GameActionResponse response, List<GameLog> actionLog) = m_gameEngine.ConsumeAction(action, userName);

            // Check if the game has ended yet.
            if(m_gameEngine.HasEnded())
            {
                // If it has, set the time and make sure we update the state.
                if(!m_gameEndedAt.HasValue)
                {
                    m_gameEndedAt = DateTime.UtcNow;
                }

                // Do this on a async thread so we don't block the caller.
                Task.Run(() => EnsureEnded());
            }

            // Broadcast the game log updates.
            BroadcastMessages(actionLog);

            // Return the response.
            return SendGameActionResponse.CreateResponse(response);
        }     

        public void GameTick()
        {
            // Do a quick state check, if we are done there's nothing to do.
            if(m_state == KokkaKoroGameState.Complete)
            {
                return;
            }

            try
            {
                GameTickInternal();
            }
            catch(Exception e)
            {
                Console.WriteLine($"Exception in game tick {e.Message}");
                SetFatalError($"Exception thrown in game tick. {e.Message}, {e.StackTrace}");
                EnsureEnded();
            }            
        }

        private void GameTickInternal()
        {
            // First, make sure the game should still be running.
            DateTime now = DateTime.UtcNow;
            if(now - m_createdAt > m_gameTimeLmit)
            {
                SetFatalError("Game timeout.");
                EnsureEnded();
                return;
            }

            if(m_state == KokkaKoroGameState.Lobby)
            {
                // If we are in the lobby, we have nothing to do right now.
                return;
            }
            if(m_state == KokkaKoroGameState.WaitingForHostedBots)
            {
                // If we are in the hosted bots connecting state also return.
                // When they connect the join call will start the game when the last one is ready.
                return;
            }
        }

        public void EnsureEnded()
        {
            // Set the game complete
            lock (m_gameLock)
            {
                if(m_state == KokkaKoroGameState.Complete)
                {
                    return;
                }
                m_state = KokkaKoroGameState.Complete;
            }

            GameEngine engine = m_gameEngine;
            if (engine != null)
            {
                if (!engine.HasEnded())
                {
                    // Make sure the game has ended, if it hasn't yet, it would be due to a timeout.
                    // Ignore the response.
                    (GameActionResponse response, List<GameLog> actionLog) = m_gameEngine.EndGame(GameCommon.Protocol.GameUpdateDetails.GameEndReason.GameTimeout);

                    // Broadcast the game log updates.
                    BroadcastMessages(actionLog);
                }
            }

            // Set the end time if not already.
            if(!m_gameEndedAt.HasValue)
            {
                m_gameEndedAt = DateTime.UtcNow;
            }

            // Get a local list of the players
            List<ServicePlayer> localPlayers = new List<ServicePlayer>();
            List<string> botUserNames = new List<string>();
            lock (m_gameLock)
            {
                foreach(ServicePlayer p in m_players)
                {
                    localPlayers.Add(p);
                    if(p.IsBot())
                    {
                        botUserNames.Add(p.GetUserName());
                    }
                }
            }

            // Make sure the bots are dead.
            try
            {
                foreach (ServicePlayer p in localPlayers)
                {
                    if (p.IsBot())
                    {
                        p.EnsureKilled();
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Error("Failed to ensure bots are dead for game.", e);
            }

            // Clean up the bot user names. This can be async, it doesn't matter since
            // we have a copy of the names.
            Task.Run(async () =>
            {
                try
                {
                    foreach (string userName in botUserNames)
                    {
                        await UserMaster.Get().RemoveUser(userName);                        
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to cleanup bot usernames.", e);
                }
            });             
        }

        public KokkaKoroGame GetInfo()
        {
            // Build a list of players.
            List<KokkaKoroPlayer> players = new List<KokkaKoroPlayer>();
            lock (m_gameLock)
            {
                foreach (ServicePlayer player in m_players)
                {
                    players.Add(player.GetInfo());
                }
            }

            return new KokkaKoroGame
            {
                State = m_state,
                Id = m_id,
                Players = players,
                GameName = m_gameName,
                CreatedBy = m_createdBy,
                HasPassword = !String.IsNullOrWhiteSpace(m_password),
                GameTimeLimitSeconds = m_gameTimeLmit.TotalSeconds,
                Created = m_createdAt,
                Started = m_startedAt,
                GameEngineStarted = m_gameStartedAt,
                Eneded = m_gameEndedAt,
                IfFailedFatialError = m_fatalError
            };
        }

        private void SetFatalError(string err)
        {
            if(String.IsNullOrWhiteSpace(err))
            {
                m_fatalError = err;
            }
        }

        private void BroadcastMessages(List<GameLog> actionLog)
        {
            if(actionLog.Count() == 0)
            {
                return;
            }

            // Use a for loop here to make sure we don't hit exceptions since we aren't using the lock.
            // It's safe to not use the lock because this will never decrease, only increase. So worst case,
            // we miss someone.
            List<string> userNames = new List<string>();
            for (int i = 0; i < m_players.Count; i++)
            {
                userNames.Add(m_players[i].GetUserName());
            }

            // Ensure all of the logs are tagged with the gameId
            foreach(GameLog l in actionLog)
            {
                l.GameId = GetId();
            }

            // Create the message
            KokkaKoroResponse<object> message = KokkaKoroResponse<object>.CreateGameLogsUpdate(actionLog);

            // Send the message
            WebsocketManager.Get().BroadcastMessage(userNames, message, true);
        }
    }
}
