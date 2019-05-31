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

        // For bots, this is the inGame name. For real users, this is the username.
        string m_userName;

        // Bot only stuff.
        ServiceBot m_bot = null;

        // Bot constructor
        public ServicePlayer(ServiceBot bot, string inNameGame)
        {
            m_userName = inNameGame;
            m_bot = bot;

            // For bots the user name must be unique so they can join the games.
            m_userName = Guid.NewGuid().ToString();
        }

        // Real user constructor
        public ServicePlayer(string userName)
        {
            m_userName = userName;
        }

        public void StartBot()
        {
            if(!IsBot())
            {
                return;
            }
            m_bot.StartBot();
        }

        public bool IsBot()
        {
            return m_bot != null;
        }

        public string GetBotName()
        {
            return m_bot == null ? null : m_bot.GetBotName();
        }

        public string GetInGameName()
        {
            return m_userName;
        }

        public KokkaKoroPlayer GetInfo()
        {
            return new KokkaKoroPlayer()
            {
                BotName = GetBotName(),
                IsBot = IsBot(),
                PlayerName = m_userName
            };
        }
    }
}
