using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServicePlayer
    {
        string m_inGameName;
        ServiceBot m_bot;
        Guid? m_userId;

        public ServicePlayer(ServiceBot bot, string inNameGame)
        {
            m_inGameName = inNameGame;
            m_bot = bot;
        }

        public ServicePlayer(Guid userId, string inNameGame)
        {
            m_userId = userId;
            m_inGameName = inNameGame;
        }

        public string GetInGameName()
        {
            return m_inGameName;
        }

        public KokkaKoroPlayer GetInfo()
        {
            return new KokkaKoroPlayer()
            {
                BotName = m_bot == null ? null : m_bot.GetBotName(),
                IsBot = m_bot != null,
                PlayerName = m_inGameName
            };
        }
    }
}
