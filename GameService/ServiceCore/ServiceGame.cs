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
        int m_playerLimit;
        string m_password;
        string m_gameName;
        string m_createdBy;
        TimeSpan m_minTurnTimeLimit;
        TimeSpan m_turnTimeLimit;
        TimeSpan m_gameTimeLmit;
        DateTime m_createdAt;

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

        public KokkaKoroGame GetInfo()
        {
            return new KokkaKoroGame
            {
                State = m_state,
                Id = m_id,
                PlayerLimit = (int)m_playerLimit,
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
