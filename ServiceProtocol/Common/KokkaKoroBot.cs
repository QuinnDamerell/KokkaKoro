using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Common
{
    public class KokkaKoroBotLog
    {
        // The bot details
        public KokkaKoroBot Bot;

        // The player details.
        public KokkaKoroPlayer Player;

        // The standard output of the bot thus far.
        public string StdOut;

        // The standard error of the bot thus far.
        public string StdErr;
    }

    public class KokkaKoroBot
    {
        // The name of the bot
        public string Name;

        // The bot version.
        public int Major;
        public int Minor;
        public int Revision;

        // The name of the dll we will run.
        public string EntryDll;

        // A password to protect the bot from uploads
        public string Password;

        // 
        // Helper functions
        // 
        public bool Equals(KokkaKoroBot o)
        {
            if(Major != o.Major || Minor != o.Minor || Revision != o.Revision)
            {
                return false;
            }
            if(Name == null || o.Name == null || Name != o.Name)
            {
                return false;
            }
            if(EntryDll == null || o.EntryDll == null || EntryDll != o.EntryDll)
            {
                return false;
            }
            if(Password == null || o.Password == null || Password != o.Password)
            {
                return false;
            }
            return true;
        }

        public static KokkaKoroBot ParseAndValidate(string json)
        {
            KokkaKoroBot bot = JsonConvert.DeserializeObject<KokkaKoroBot>(json);
            return (bot != null && bot.IsValid()) ? bot : null;  
        }

        public bool IsValid()
        {
            return !String.IsNullOrWhiteSpace(Name)
                && !String.IsNullOrWhiteSpace(EntryDll)
                && EntryDll.ToLower().EndsWith(".dll")
                && Major >= 0
                && Minor >= 0
                && Revision >= 0;
        }
    }
}
