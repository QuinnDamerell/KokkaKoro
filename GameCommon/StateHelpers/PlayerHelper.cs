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
            if(s.Players == null)
            {
                return "Players object is null";
            }
            if(s.Players.Count == 0)
            {
                return "There are no players";
            }
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
                if(p.OwnedBuildings == null)
                {
                    return $"Player {p.UserName} owned buildings object is null";
                }
                if(p.OwnedBuildings.Count != m_gameHelper.BuildingRules.GetCountOfUniqueTypes())
                {
                    return $"Player {p.UserName}'s owned building list is too short";
                }
                foreach(int i in p.OwnedBuildings)
                {
                    if(i < 0)
                    {
                        return $"Player {p.UserName}'s building count for building {i} is < 0";
                    }
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

        public int GetMaxRollsAllowed(string userName = null)
        {
            return 1;
        }

        public int GetMaxDiceCountCanRoll(string userName = null)
        {
            return 1;
        }
    }
}

