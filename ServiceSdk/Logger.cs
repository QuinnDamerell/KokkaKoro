using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceSdk
{
    class Logger
    {
        static bool m_debug = false;

        public static void SetDebug(bool state)
        {
            m_debug = state;
        }

        public static void Info(string msg)
        {
            Write($"[Info] {msg}");
        }

        public static void Error(string msg, Exception e = null)
        {
            Write($"[ERR] {msg} - { (e != null ? e.Message : "") }");
        }

        static void Write(string msg)
        {
            if (!m_debug)
            {
                return;
            }
            Console.WriteLine(msg);
        }
    }
}
