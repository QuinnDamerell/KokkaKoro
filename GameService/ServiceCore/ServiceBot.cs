using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.ServiceCore
{
    public class ServiceBot
    {
        KokkaKoroBot m_info;
        string m_localPath;

        public ServiceBot(KokkaKoroBot info, string localPath)
        {
            m_info = info;
            m_localPath = localPath;
        }

        public string GetExePath()
        {
            return $"{m_localPath}/{m_info.EntryDll}";
        }

        public string GetBotName()
        {
            return m_info.Name;
        }
    }
}
