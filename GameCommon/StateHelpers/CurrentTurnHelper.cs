using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class CurrentTurnHelper
    {
        StateHelper m_gameHelper;

        internal CurrentTurnHelper(StateHelper gameHelper)
        {
            m_gameHelper = gameHelper;
        }

        public string Validate()
        {
            GameState s = m_gameHelper.GetState();
            if (s.CurrentTurnState.PlayerIndex < 0 || s.CurrentTurnState.PlayerIndex >= s.Players.Count)
            {
                return "Current turn state player index is out of bounds.";
            }
            if (s.CurrentTurnState.Rolls < 0 || s.CurrentTurnState.Rolls > m_gameHelper.Player.MaxRollsAllowed())
            {
                return "The current turn state rolls is to high or low for the current player.";
            }
            if(s.CurrentTurnState.Rolls > 0 && s.CurrentTurnState.DiceResults.Count == 0)
            {
                return "The current turn has rolls but no dice value.";
            }
            if(s.CurrentTurnState.DiceResults.Count > m_gameHelper.Player.MaxDiceCountCanRoll())
            {
                return "There are more dice results than then player can currently roll dice.";
            }
            return null;
        }

        public bool IsMyTurn(string userName = null)
        {
            GameState s = m_gameHelper.GetState();
            return s.CurrentTurnState.PlayerIndex == m_gameHelper.Player.GetPlayerIndex(userName);
        }

        public bool CanRollOrReRoll()
        {
            GameState s = m_gameHelper.GetState();
            return s.CurrentTurnState.Rolls < m_gameHelper.Player.MaxRollsAllowed();
        }

        public bool CanBuyABuilding()
        {
            GameState s = m_gameHelper.GetState();
            return !s.CurrentTurnState.HasBougthBuilding;
        }

        public string GetActiveTurnPlayerUserName()
        {
            GameState s = m_gameHelper.GetState();
            return m_gameHelper.Player.GetPlayerUserName(s.CurrentTurnState.PlayerIndex);
        }

        public List<GameActionType> GetPossibleActions()
        {
            List<GameActionType> actions = new List<GameActionType>();
            GameState s = m_gameHelper.GetState();
            if(s.CurrentTurnState.Rolls == 0)
            {
                // If there have been no rolls, the only action is to make a roll.
                actions.Add(GameActionType.RollDice);
                return actions;
            }

            if(CanRollOrReRoll())
            {
                // If the user can roll more than once, list it as an option.
                actions.Add(GameActionType.RollDice);
            }

            if(CanBuyABuilding())
            {
                actions.Add(GameActionType.BuyBuilding);
            }

            return actions;
        }

        public bool CanTakeAction(GameActionType type)
        {
            return GetPossibleActions().Contains(type);
        }
    }
}
