using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
        object m_stateLock = new object();
        int m_playerLimit;
        string m_password;
        string m_gameName;
        string m_createdBy;
        TimeSpan m_minTurnTimeLimit;
        TimeSpan m_turnTimeLimit;
        TimeSpan m_gameTimeLmit;
        DateTime m_createdAt;

        List<ServicePlayer> m_players = new List<ServicePlayer>();

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

        public string AddPlayer(string playerName, Guid? botId, Guid? userId)
        {
            if(!botId.HasValue && !userId.HasValue)
            {
                return "No bot or user specified.";
            }

            lock(m_stateLock)
            {
                if(m_state != KokkaKoroGameState.Lobby)
                {
                    return "Game not in joinable state.";
                }
            }

            lock (m_players)
            {
                if (m_players.Count() >= m_playerLimit)
                {
                    return "Game full";
                }
                m_players.Add(new ServicePlayer(botId, userId, playerName));
            }
            return null;
        }

        public string StartGame()
        {
            lock(m_players)
            {
                if(m_players.Count < 1)
                {
                    return "There must be at least a player to start the game.";
                }
            }

            lock (m_stateLock)
            {
                if (m_state != KokkaKoroGameState.Lobby)
                {
                    return "Invalid state to start game";
                }
                m_state = KokkaKoroGameState.InProgress;
            }

            // TODO STUFF

            return null;
        }

        public KokkaKoroGame GetInfo()
        {
            // Build a list of players.
            List<KokkaKoroPlayer> players = new List<KokkaKoroPlayer>();
            lock (m_players)
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
    }
}
