using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServiceGame
    {
        static TimeSpan c_maxGameLength = new TimeSpan(1, 0, 0);
        static TimeSpan c_maxTurnTime   = new TimeSpan(1, 0, 0);
        static TimeSpan c_minTurnTime   = new TimeSpan(0, 0, 0);

        Guid m_id;
        KokkaKoroGameState m_state;
        object m_gameLock = new object();
        int m_playerLimit;
        string m_password;
        string m_gameName;
        string m_createdBy;
        TimeSpan m_minTurnTimeLimit;
        TimeSpan m_turnTimeLimit;
        TimeSpan m_gameTimeLmit;
        DateTime m_createdAt;

        string m_fatalError = null;

        List<ServicePlayer> m_players = new List<ServicePlayer>();
        Thread m_gameLoop = null;

        public ServiceGame(int? playerLimit = null, string gameName = null, string createdBy = null, string password = null, TimeSpan? turnTimeLimit = null, TimeSpan? minTurnLimit = null, TimeSpan? gameTimeLimit = null)
        {
            m_state = KokkaKoroGameState.Lobby;
            m_id = Guid.NewGuid();
            m_createdAt = DateTime.UtcNow;
            m_gameTimeLmit = gameTimeLimit.HasValue ? gameTimeLimit.Value : TimeSpan.MaxValue;
            m_turnTimeLimit = turnTimeLimit.HasValue ? turnTimeLimit.Value : TimeSpan.MaxValue;
            m_minTurnTimeLimit = minTurnLimit.HasValue ? minTurnLimit.Value : TimeSpan.MinValue;
            m_gameName = String.IsNullOrWhiteSpace(gameName) ? $"game-{m_id}" : gameName;
            m_createdBy = String.IsNullOrWhiteSpace(gameName) ? $"unknown" : createdBy;
            m_password = String.IsNullOrWhiteSpace(password) ? null : password;
            m_playerLimit = playerLimit.HasValue ? playerLimit.Value : 4;

            // Limit the game length.
            if(m_gameTimeLmit > c_maxGameLength)
            {
                m_gameTimeLmit = c_maxGameLength;
            }
            if(m_turnTimeLimit > c_maxTurnTime)
            {
                m_turnTimeLimit = c_maxTurnTime;
            }
            if(m_minTurnTimeLimit < c_minTurnTime)
            {
                m_minTurnTimeLimit = c_minTurnTime;
            }
        }

        public Guid GetId()
        {
            return m_id;
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
                if (m_players.Count() >= m_playerLimit)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }
            }

            // Download the bot locally.
            ServiceBot bot;
            try
            {
                bot = await StorageMaster.Get().DownloadBot(botName);
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
                if (m_players.Count() >= m_playerLimit)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }

                // Make sure the name is unique, for fun.
                int count = 0;
                while(count < m_players.Count())
                {
                    if(m_players[count].GetInGameName() == inGameName)
                    {
                        // Append an ending and restart.
                        Random r = new Random();
                        switch(r.Next(0, 5))
                        {
                            case 0:
                                inGameName += "est";
                                break;
                            case 1:
                                inGameName += "er";
                                break;
                            case 2:
                                inGameName = "Dr "+ inGameName;
                                break;
                            case 3:
                                inGameName += " the second";
                                break;
                            case 4:
                                inGameName += " the great";
                                break;
                        }
                        count = 0;
                    }
                    count++;
                }

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

            ServicePlayer foundHostedBot = null;
            lock (m_gameLock)
            {
                // Check the state again.
                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game not in joinable state");
                }
                if (m_players.Count() >= m_playerLimit)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }

                // Search to see if this is a bot joining the game.
                foreach(ServicePlayer p in m_players)
                {
                    if(p.GetUserName().Equals(userName))
                    {
                        // This is a bot connecting, let the service player know it has now joined.
                        foundHostedBot = p;
                        break;
                    }
                }

                // If this isn't a bot connecting to the game,
                // add the new player.
                if(foundHostedBot == null)
                {
                    return KokkaKoroResponse<object>.CreateError("Remote players are not supported at this time.");
                    // Add the player.
                    //m_players.Add(new ServicePlayer(bot, inGameName));
                }
            }

            // Outside of the lock, if this was a hosted bot connecting tell it now.
            if(foundHostedBot != null)
            {
                foundHostedBot.SetBotJoined();
            }

            // Create a response
            JoinGameResponse response = new JoinGameResponse()
            {
                Game = GetInfo()
            };
            return KokkaKoroResponse<object>.CreateResult(response);
        }

        public string StartGame()
        {
            lock (m_gameLock)
            {
                if (m_players.Count < 1)
                {
                    return "There must be at least a player to start the game.";
                }

                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return "Invalid state to start game";
                }
                m_state = KokkaKoroGameState.WaitingForHostedBots;
            }

            // Kick off the game loop thread to run the game.
            m_gameLoop = new Thread(GameLoop);
            m_gameLoop.Start();

            return null;
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
                PlayerLimit = (int)m_playerLimit,
                Players = players,
                GameName = m_gameName,
                CreatedBy = m_createdBy,
                HasPassword = !String.IsNullOrWhiteSpace(m_password),
                TurnTimeLimitSeconds = m_turnTimeLimit.TotalSeconds,
                MinTurnTimeLimitSeconds = m_minTurnTimeLimit.TotalSeconds,
                GameTimeLimitSeconds = m_gameTimeLmit.TotalSeconds,
                Created = m_createdAt
            };
        }

        private void GameLoop()
        {
            try
            {
                InnerGameLoop();
            }
            catch(Exception e)
            {
                Logger.Error("Excption in game loop.", e);
                SetFatalError($"Exception thrown in game loop. {e.Message}");
            }

            // Make sure our state is set.
            lock (m_gameLock)
            {
                m_state = KokkaKoroGameState.Complete;
            }            
        }

        private void InnerGameLoop()
        {
            lock(m_gameLock)
            {
                if(m_state != KokkaKoroGameState.WaitingForHostedBots)
                {
                    SetFatalError("Wrong inital state.");
                    return;
                }
            }

            // Spawn the bot players.
            foreach(ServicePlayer p in m_players)
            {
                if(p.IsBot())
                {
                    p.StartBot();
                }
            }

            // Wait for the bots to connect.


        }

        private void SetFatalError(string err)
        {
            if(String.IsNullOrWhiteSpace(err))
            {
                m_fatalError = err;
            }
        }
    }
}
