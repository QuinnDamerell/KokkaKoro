using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServicePlayer
    {
        static readonly string s_botPassword = "IamABot";

        // Shared for both bots and real users.
        string m_userName;

        // Bot only stuff.
        bool m_isReady = false;
        string m_inGameName;
        ServiceBot 
            = null;

        public ServicePlayer(ServiceBot bot, string inNameGame)
        {
            m_inGameName = inNameGame;
            m_bot = bot;

            // For bots the user name must be unique so they can join the games.
            m_userName = Guid.NewGuid().ToString();
        }

        public ServicePlayer(string userName)
        {
            m_userName = userName;
        }

        public string GetInGameName()
        {
            return m_bot != null ? m_inGameName : m_userName;
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
