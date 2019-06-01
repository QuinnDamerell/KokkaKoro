using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServicePlayer
    {
        // For hosted bots, this is a GUID we make that we will give to the bot for them to connect with.
        // For remote players, this is the actual username.
        string m_userName;

        // Bot only stuff.
        ServiceBot m_bot = null;
        string m_inGameName;

        // Bot constructor
        public ServicePlayer(ServiceBot bot, string inNameGame)
        {
            m_inGameName = inNameGame;
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

        public void SetBotJoined()
        {
            if(!IsBot())
            {
                return;
            }
            m_bot.SetBotJoined();
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
            return m_inGameName;
        }

        public string GetUserName()
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
