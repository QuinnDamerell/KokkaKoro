using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class PlayerHelper
    {
        StateHelper m_gameHelper;

        internal PlayerHelper(StateHelper gameHelper)
        {
            m_gameHelper = gameHelper;
        }

        public string Validate()
        {
            GameState s = m_gameHelper.GetState();
            foreach (GamePlayer p in s.Players)
            {
                if (String.IsNullOrWhiteSpace(p.Name) || String.IsNullOrWhiteSpace(p.UserName))
                {
                    return "A user has a empty name or username.";
                }
                if (p.Coins < 0)
                {
                    return $"User {p.UserName} has less than 0 coins";
                }
            }
            return null;
        }

        public int GetPlayerIndex(string userName = null)
        {
            string matchUserName = userName == null ? m_gameHelper.GetPerspectiveUserName() : userName;
            GameState s = m_gameHelper.GetState();
            for (int i = 0; i < s.Players.Count; i++)
            {
                if (s.Players[i].UserName.Equals(matchUserName))
                {
                    return i;
                }
            }
            return -1;
        }

        public string GetPlayerUserName(int index = -1)
        {
            GameState s = m_gameHelper.GetState();
            if (index < -1 || index >= s.Players.Count)
            {
                return null;
            }
            if (index < -1)
            {
                index = GetPlayerIndex();
            }
            return s.Players[index].UserName;
        }

        public int MaxRollsAllowed(string userName = null)
        {
            return 1;
        }

        public int MaxDiceCountCanRoll(string userName = null)
        {
            return 1;
        }
    }
}

