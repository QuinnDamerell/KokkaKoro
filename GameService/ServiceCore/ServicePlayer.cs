using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServicePlayer
    {
        const string c_botPassword = "IamABot";

        // For hosted bots, this is a GUID we make that we will give to the bot for them to connect with.
        // For remote players, this is the actual username.
        readonly string m_userName;

        // Bot only stuff.
        readonly ServiceBot m_bot = null;
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
            m_inGameName = userName;
        }

        public void StartBot(Guid gameId, string gamePassword)
        {
            if(!IsBot())
            {
                return;
            }
            m_bot.StartBot(gameId, gamePassword, m_userName, c_botPassword);
        }

        public void EnsureKilled()
        {
            if (!IsBot())
            {
                return;
            }
            m_bot.Kill();
        }

        public void SetBotJoined()
        {
            if(!IsBot())
            {
                return;
            }
            m_bot.SetBotJoined();
        }

        public bool IsReady()
        {
            if(!IsBot())
            {
                return true;
            }
            return m_bot.IsReady();
        }

        public bool IsBot()
        {
            return m_bot != null;
        }

        public string GetBotName()
        {
            return m_bot?.GetBotName();
        }

        public string GetInGameName()
        {
            return m_inGameName;
        }

        public void SetInGameName(string name)
        {
            m_inGameName = name;
        }

        public string GetUserName()
        {
            return m_userName;
        }

        public KokkaKoroBot GetBotInfo()
        {
            if (!IsBot())
            {
                return null;
            }
            return m_bot.GetBotInfo();
        }

        public string GetBotStdOut()
        {
            if (!IsBot())
            {
                return null;
            }
            return m_bot.GetStdOut();
        }

        public string GetBotStdErr()
        {
            if (!IsBot())
            {
                return null;
            }
            return m_bot.GetStdErr();
        }

        public KokkaKoroPlayer GetInfo()
        {
            return new KokkaKoroPlayer()
            {
                IsBot = IsBot(),
                PlayerName = m_userName,
                IsReady = (IsBot() ? m_bot.IsReady() : true),
                BotDetails = (IsBot() ? m_bot.GetBotPlayerDetails() : null)               
            };
        }
    }
}
