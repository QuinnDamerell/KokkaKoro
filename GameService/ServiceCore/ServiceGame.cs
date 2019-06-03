﻿using GameCore;
using GameCore.CommonObjects.Protocol;
using GameService.WebsocketsHelpers;
using Newtonsoft.Json;
using ServiceProtocol;
using ServiceProtocol.Common;
using ServiceProtocol.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServiceGame
    {
        static TimeSpan c_maxGameLength = new TimeSpan(0, 10, 0);
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

        // Game stuff
        GameEngine m_gameEngine;

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

                // Check the state again.
                if ((foundHostedBot == null && m_state != KokkaKoroGameState.Lobby) || (foundHostedBot != null && m_state != KokkaKoroGameState.WaitingForHostedBots))
                {
                    return KokkaKoroResponse<object>.CreateError($"Game not in joinable state");
                }
                if (m_players.Count() >= m_playerLimit)
                {
                    return KokkaKoroResponse<object>.CreateError($"Game full");
                }

                // If this isn't a bot connecting to the game,
                // add the new player.
                if (foundHostedBot == null)
                {
                    // Add the player.
                    m_players.Add(new ServicePlayer(userName));
                }
            }

            // Outside of the lock, if this was a hosted bot connecting tell it now.
            if(foundHostedBot != null)
            {
                foundHostedBot.SetBotJoined();

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

                // Put the game in the waiting for bots state.
                m_state = KokkaKoroGameState.WaitingForHostedBots;
            }

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
            lock(m_gameLock)
            {
                if(m_state != KokkaKoroGameState.Lobby && m_state != KokkaKoroGameState.WaitingForHostedBots)
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
            List<string> players = new List<string>();
            foreach(ServicePlayer p in m_players)
            {
                players.Add(p.GetInGameName());
            }
            m_gameEngine = new GameEngine(players, GameMode.BaseGame);

            // Invoke the first null action to get the ball rolling.
            List<GameLog> actionLog = m_gameEngine.ConsumeAction();

            // Send the messages generated by the action to the players.
            BroadcastMessages(actionLog);
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
                // Check if the bots are ready now.
                foreach(ServicePlayer  p in m_players)
                {
                    if(!p.IsReady())
                    {
                        return;
                    }
                }

                // We are ready! Start the game!
                StartGameInternal();
            }
        }

        public void EnsureEnded()
        {
            try
            {
                // Make sure the bots are dead.
                foreach (ServicePlayer p in m_players)
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

            // Set the game complete
            lock (m_gameLock)
            {
                m_state = KokkaKoroGameState.Complete;
            }
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

        private void SetFatalError(string err)
        {
            if(String.IsNullOrWhiteSpace(err))
            {
                m_fatalError = err;
            }
        }

        private void BroadcastMessages(List<GameLog> actionLog)
        {
            // Use a for loop here to make sure we don't hit exceptions since we aren't using the lock.
            // It's safe to not use the lock because this will never decrease, only increase. So worst case,
            // we miss someone.
            List<string> userNames = new List<string>();
            for (int i = 0; i < m_players.Count; i++)
            {
                userNames.Add(m_players[i].GetUserName());
            }

            // For ever log message we got...
            foreach (GameLog log in actionLog)
            {
                // Create the message
                KokkaKoroResponse<object> message = KokkaKoroResponse<object>.CreateGameLogUpdate(log);           

                // Send the message
                WebsocketManager.Get().BroadcastMessage(userNames, message, true);
            }
        }
    }
}
