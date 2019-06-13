using GameCommon.StateHelpers;
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
            // Get a local list of games
            List<ServiceGame> games = null;
            lock(m_games)
            {
                games = new List<ServiceGame>(m_games);
            }

            // Now get the game info
            List<KokkaKoroGame> gameInfo = new List<KokkaKoroGame>();
            foreach(ServiceGame g in games)
            {
                gameInfo.Add(g.GetInfo());
            }

            // Now figure out the current bot results.
            Dictionary<string, TournamentResult> botRank = new Dictionary<string, TournamentResult>();
            foreach(KokkaKoroGame g in gameInfo)
            {
                if(!g.Eneded.HasValue)
                {
                    // If the game hasn't been ended, add them to the pending game.
                    foreach(KokkaKoroPlayer p in g.Players)
                    {
                        if(p.IsBot)
                        {
                            GetBotResult(botRank, p.BotDetails.Bot.Name).InProgress++;
                        }
                    }
                }
                else
                {
                    if(!g.HasWinner || g.Leaderboard == null || g.Leaderboard.Count == 0)
                    {
                        // If the game doesn't have a winner, add to the error list.
                        foreach (KokkaKoroPlayer p in g.Players)
                        {
                            if (p.IsBot)
                            {
                                GetBotResult(botRank, p.BotDetails.Bot.Name).InProgress++;
                            }
                        }
                    }
                    else
                    {
                        // The game is over and has a leader board, add the data.

                        // Only consider the highest ranking bot in each game (if there's the same bot in multiple games.)
                        Dictionary<string, bool> currentGame = new Dictionary<string, bool>();
                        foreach (KokkaKoroLeaderboardElement l in g.Leaderboard)
                        {
                            if (!currentGame.ContainsKey(l.Player.Name))
                            {
                                currentGame.Add(l.Player.Name, false);
                                TournamentResult result = GetBotResult(botRank, l.Player.Name);
                                if (l.Rank == 1)
                                {
                                    result.Wins++;
                                }
                                else
                                {
                                    result.Losses++;
                                }
                                // 3 points for 1st, 2 points for 2nd, 1 points for 3rd, and 0 for 4th.
                                result.Score += Math.Max(0, 4 - l.Rank);
                            }
                        }
                    }
                }
            }

            // Build the final list and sort them.
            List<TournamentResult> results = new List<TournamentResult>();
            foreach(KeyValuePair<string, TournamentResult> p in botRank)
            {
                bool added = false;
                for(int i = 0; i < results.Count; i++)
                {
                    if (results[i].Losses != 0 && results[i].Wins != 0)
                    {
                        results[i].WinRate = ((double)results[i].Wins / (double)(results[i].Losses + results[i].Wins)) * 100.0;
                    }
                    else
                    {
                        results[i].WinRate = 0;
                    }

                    if(results[i].Score < p.Value.Score)
                    {
                        results.Insert(i, p.Value);
                        added = true;
                        break;
                    }
                }
                if(!added)
                {
                    results.Add(p.Value);
                }
            }

            return new KokkaKoroTournament()
            {
                Id = m_id,
                Status = m_status,
                MessageIfError = m_error,
                CreatedFor = m_createdBy,
                Reason = m_reason,
                Games  = gameInfo,
                Results = results
            };
        }

        private TournamentResult GetBotResult(Dictionary<string, TournamentResult> botRank, string botName)
        {
            if (!botRank.ContainsKey(botName))
            {
                botRank[botName] = new TournamentResult() { BotName = botName };
            }
            return botRank[botName];
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
