using ServiceProtocol.Common;
using ServiceSdk;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ServiceUtility
{
    public enum Log
    {
        Info,
        Warn,
        Error,
        Fatial
    }

    class Logger : ILogger
    {
        int m_indent = 0;

        public void Info(string msg)
        {
            Write($"", msg);
        }

        public void Info(string msg = "", bool newLine = true)
        {
            Write($"", msg, newLine);
        }

        public void Warn(string msg)
        {
            Write($" [Warn]", msg);
        }

        public void Error(string msg, Exception e = null)
        {
            Write($" [!ERR!]", $"{msg} - Message: {(e == null ? "" : e.Message)}");
        }

        public void Fatial(string msg, Exception e = null)
        {
            Write($" [!!CRIT!!]", $"{msg} - Message: {(e == null ? "" : e.Message)}");
        }

        public void SetIndent(int indent)
        {
            m_indent = indent;
        }

        public void IncreaseIndent()
        {
            m_indent++;
        }

        public void DecreaseIndent()
        {
            m_indent--;
        }

        private void Write(string preIndent, string afterIndent, bool newLine = true)
        {
            string str = $"[{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)}]{preIndent}";
            for(int i = 0; i < m_indent; i++)
            {
                str += "  ";
            }
            str += $" {afterIndent}";
            if(newLine)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.Write(str);
            }
        }

        public int GetInt(string message, int min, int max)
        {
            while (true)
            {
                Info($"{message}: ", false);
                string value = Console.ReadLine();
                if (int.TryParse(value, out var result))
                {
                    if (result >= min && result <= max)
                    {
                        return result;
                    }
                }
                Info("Invalid option, try again.");
            }
        }

        public string GetString(string message)
        {
            while (true)
            {
                Info($"{message}: ", false);
                string value = Console.ReadLine();
                if (!String.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
                Info("Invalid option, try again.");
            }
        }

        public bool GetDecission(string message)
        {
            while (true)
            {
                Info($"{message}? [y or n] ", false);
                string value = Console.ReadLine();
                if (value.Trim().ToLower() == "y")
                {
                    return true;
                }
                if (value.Trim().ToLower() == "n")
                {
                    return false;
                }
                Info("Invalid option, try again.");
            }
        }
    }
}
