using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    // State helper is a read-only class that helps answer current state questions.
    // The state helper takes a perspective username, any functions called without specifying the user
    // will assume the perspective username.
    public class StateHelper
    {
        //
        // Public vars
        //
        public PlayerHelper Player;
        public CurrentTurnHelper CurrentTurn;

        // 
        // Internal vars
        // 
        readonly string m_perspectiveUserName;
        readonly GameState m_state;

        public StateHelper(GameState state, string fromPerspectiveUserName)
        {
            Player = new PlayerHelper(this);
            CurrentTurn = new CurrentTurnHelper(this);

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
            if(m_state.Market == null || m_state.Players == null || m_state.CurrentTurnState == null)
            {
                return "State objects are null.";
            }
            if(m_state.Players.Count == 0)
            {
                return "There are no players";
            }
            if(Player.GetPlayerIndex() == -1)
            {
                return "The given perspective user name wasn't found in the players.";
            }

            // Let the helpers validate the sub objects.
            string response = Player.Validate();
            if(!String.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            response = CurrentTurn.Validate();
            if (!String.IsNullOrWhiteSpace(response))
            {
                return response;
            }
            return null;
        }
    }
}
