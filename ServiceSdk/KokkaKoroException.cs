using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceSdk
{
    public class KokkaKoroException : Exception
    {
        string m_message;
        bool m_isFromService;

        public KokkaKoroException(string msg, bool isFromService)
            : base(msg)
        {
            m_message = msg;
            m_isFromService = isFromService;
        }

        public bool IsFromService()
        {
            return m_isFromService;
        }
    }
}
