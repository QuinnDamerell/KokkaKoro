using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    /// <summary>
    /// State helper is a read-only class that helps answer current state questions.
    /// The state helper takes a perspective user name, any functions called without specifying the user
    /// will assume the perspective user name.
    /// </summary>
    public class StateHelper
    {
        //
        // Public vars
        //

        /// <summary>
        /// Helper functions to answer player questions.
        /// </summary>
        public PlayerHelper Player;

        /// <summary>
        /// Helper functions to answer current turn questions.
        /// </summary>
        public CurrentTurnHelper CurrentTurn;

        /// <summary>
        /// Helper functions to answer marketplace questions.
        /// </summary>
        public MarketplaceHelper Marketplace;

        /// <summary>
        /// Gives access to the building rules.
        /// </summary>
        public BuildingRules BuildingRules;

        // 
        // Internal vars
        // 
        readonly GameState m_state;

        /// <summary>
        /// Creates a new state helper given a user name perspective.
        /// To get a StateHelper object, use the GameState GetStateHelper function.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="fromPerspectiveUserName"></param>
        internal StateHelper(GameState state, string fromPerspectiveUserName)
        {
            m_state = state;
            Player = new PlayerHelper(this, fromPerspectiveUserName);
            CurrentTurn = new CurrentTurnHelper(this);
            Marketplace = new MarketplaceHelper(this);
            if (state != null)
            {
                BuildingRules = new BuildingRules(state.Mode);
            }
        }

        /// <summary>
        /// Returns the current game state.
        /// </summary>
        /// <returns></returns>
        public GameState GetState()
        {
            return m_state;
        }

        /// <summary>
        /// Ensures that all of the state is valid.
        /// </summary>
        /// <returns>An error string if something is wrong.</returns>
        public string Validate()
        {
            // Validate base object things.
            if(m_state == null)
            {
                return "We don't have a state.";
            }
            if(m_state.Market == null || m_state.Players == null || m_state.CurrentTurnState == null || BuildingRules == null)
            {
                return "State objects are null.";
            }
            if(m_state.Players.Count == 0)
            {
                return "There are no players";
            }
            if(Player.GetPlayer() == null)
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

            response = Marketplace.Validate();
            if (!String.IsNullOrWhiteSpace(response))
            {
                return response;
            }            
            return null;
        }
    }
}
