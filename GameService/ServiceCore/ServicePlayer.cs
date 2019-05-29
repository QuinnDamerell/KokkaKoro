using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServicePlayer
    {
        string m_playerName;
        Guid? m_botId;
        Guid? m_userId;

        public ServicePlayer(Guid? botId, Guid? userId, string playerName)
        {
            m_botId = botId;
            m_userId = userId;
            m_playerName = playerName;
        }

        public KokkaKoroPlayer GetInfo()
        {
            return new KokkaKoroPlayer()
            {
                BotId = m_botId,
                IsBot = m_botId != null,
                PlayerName = m_playerName
            };
        }
    }
}
