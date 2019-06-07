using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore
{
    class LogKeeper
    {
        List<GameLog> m_currentLog = new List<GameLog>();

        public void AddToLog(List<GameLog> newLogs)
        {
            lock (m_currentLog)
            {
                m_currentLog.AddRange(newLogs);
            }
        }

        public List<GameLog> GetLogs()
        {
            lock (m_currentLog)
            {
                // Return a copy of the list.
                // This won't make copies of the GameLogs, but only the list.
                // The game log objects won't change once they are in the list.
                return new List<GameLog>(m_currentLog);
            }
        }
    }
}
