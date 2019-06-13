using ServiceProtocol;
using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServiceTournament
    {
        Guid m_id;
        TournamentStatus m_status;
        int m_numberOfGames;
        int m_botsPerGame;
        List<string> m_botsToUse;
        string m_reason;
        string m_createdBy;
        DateTime m_createdAt;
        string m_error;
        Thread m_thread;
        List<ServiceGame> m_games = new List<ServiceGame>();

        public ServiceTournament(int gameCount, int playersPerGame, string reason, string createdBy, List<string> bots)
        {
            m_id = Guid.NewGuid();
            m_status = TournamentStatus.Created;
            m_numberOfGames = gameCount;
            m_botsPerGame = playersPerGame;
            m_botsToUse = bots;
            m_reason = reason;
            m_createdBy = createdBy;
            m_createdAt = DateTime.UtcNow;
        }

        public void Start()
        {
            m_status = TournamentStatus.Running;
            m_thread = new Thread(WorkerThread);
            m_thread.Start();
        }

        public Guid GetId()
        {
            return m_id;
        }

        private async void WorkerThread()
        {
            try
            {
                if(await DoWork())
                {
                    m_status = TournamentStatus.Complete;
                }
                else
                {
                    m_status = TournamentStatus.Error;
                }
            }
            catch(Exception e)
            {
                SetError($"Exception in worker thread. {e.Message}");
                m_status = TournamentStatus.Error;
            }
        }

        private async Task<bool> DoWork()
        {
            // Make games
            int botIndex = 0;
            GameMaster gm = GameMaster.Get();
            for (int g = 0; g < m_numberOfGames; g++)
            {
                ServiceGame game = gm.CreateGame("Game" + g, null, null, "TournamentController");        
                for (int c = 0; c < m_botsPerGame; c++)
                {
                    KokkaKoroResponse<object> response = await game.AddHostedBot(m_botsToUse[botIndex], m_botsToUse[botIndex]);
                    if (!String.IsNullOrWhiteSpace(response.Error))
                    {
                        SetError(response.Error);
                        return false;
                    }

                    // Get ready for the next bot.
                    botIndex++;
                    if (botIndex >= m_botsToUse.Count)
                    {
                        botIndex = 0;
                    }
                }

                // Start the game.
                string error = game.StartGame();
                if(!String.IsNullOrWhiteSpace(error))
                {
                    SetError(error);
                    return false;
                }

                // Add the game.
                lock (m_games)
                {
                    m_games.Add(game);
                }  
            }
            return true;
        }

        public KokkaKoroTournament GetInfo()
        {
            return new KokkaKoroTournament()
            {
                Id = m_id,
                Status = m_status,
                CreatedFor = m_createdBy,
                Reason = m_reason
            };
        }

        private void SetError(string message)
        {
            if(String.IsNullOrWhiteSpace(m_error))
            {
                m_error = message;
            }
        }
    }
}
