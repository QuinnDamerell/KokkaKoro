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
            m_currentLog.AddRange(newLogs);
        }
    }
}
