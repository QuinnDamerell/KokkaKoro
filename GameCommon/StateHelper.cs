using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{


    // State helper is a read-only class
    public class StateHelper
    {
        //
        // Public vars
        //
        public PlayerHelper Player;

        // 
        // Internal vars
        // 
        readonly string m_perspectiveUserName;
        readonly GameState m_state;

        public StateHelper(GameState state, string fromPerspectiveUserName)
        {
            Player = new PlayerHelper(this);
            m_state = state;
            m_perspectiveUserName = fromPerspectiveUserName;
        }

        public GameState GetState()
        {
            return m_state;
        }

        public string GetPerspectiveUserName()
        {
            return m_perspectiveUserName;
        }

        public string Validate()
        {
            // Validate base object things.
            if(m_state == null)
            {
                return "We don't have a state.";
            }
            if(String.IsNullOrWhiteSpace(m_perspectiveUserName))
            {
                return "No perspective user name";
            }
            if(m_state.Market == null || m_state.Players == null)
            {
                return "State objects are null.";
            }
            if(m_state.Players.Count == 0)
            {
                return "There are no players";
            }
            if(m_state.CurrentPlayerIndex < 0 || m_state.CurrentPlayerIndex >= m_state.Players.Count)
            {
                return "Current player index is too high or too low.";
            }
            if(Player.GetPerspectivePlayerIndex() == -1)
            {
                return "The given perspective user name wasn't found in the players.";
            }

            // Let the helpers validate the sub objects.
            string response = Player.Validate();
            if(!String.IsNullOrWhiteSpace(response))
            {
                return response;
            }
            return null;
        }

        public class PlayerHelper
        {
            StateHelper m_gameHelper;

            internal PlayerHelper(StateHelper gameHelper)
            {
                m_gameHelper = gameHelper;
            }

            public string Validate()
            {
                foreach(GamePlayer p in m_gameHelper.GetState().Players)
                {
                    if(String.IsNullOrWhiteSpace(p.Name) || String.IsNullOrWhiteSpace(p.UserName))
                    {
                        return "A user has a empty name or username.";
                    }
                    if(p.Coins < 0)
                    {
                        return $"User {p.UserName} has less than 0 coins";
                    }
                }
                return null;
            }

            public bool IsMyTurn(string userName = null)
            {
                GameState s = m_gameHelper.GetState();
                return s.CurrentPlayerIndex == GetPlayerIndex(userName);
            }

            public int GetPlayerIndex(string userName = null)
            {
                string matchUserName = userName == null ? m_gameHelper.GetPerspectiveUserName() : userName;
                GameState s = m_gameHelper.GetState();
                for (int i = 0; i < s.Players.Count; i++)
                {
                    if(s.Players[i].UserName.Equals(matchUserName))
                    {
                        return i;
                    }
                }
                return -1;
            }

            public string GetActiveTurnPlayerUserName()
            {
                GameState s = m_gameHelper.GetState();
                return s.Players[s.CurrentPlayerIndex].UserName;
            }
        }
    }
}
