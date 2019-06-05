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
            if (s.CurrentTurnState.Rolls < 0 || s.CurrentTurnState.Rolls > m_gameHelper.Player.GetMaxRollsAllowed())
            {
                return "The current turn state rolls is to high or low for the current player.";
            }
            if(s.CurrentTurnState.Rolls > 0 && s.CurrentTurnState.DiceResults.Count == 0)
            {
                return "The current turn has rolls but no dice value.";
            }
            if(s.CurrentTurnState.DiceResults.Count > m_gameHelper.Player.GetMaxDiceCountCanRoll())
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
            return !HasEndedTurn() && !HasCommittedToDiceResult() && s.CurrentTurnState.Rolls < m_gameHelper.Player.GetMaxRollsAllowed();
        }

        public bool HasBuildABuiding()
        {
            GameState s = m_gameHelper.GetState();
            return s.CurrentTurnState.HasBougthBuilding;
        }

        public bool HasCommittedToDiceResult()
        {
            GameState s = m_gameHelper.GetState();
            return s.CurrentTurnState.HasCommitedDiceResult;
        }

        public bool HasEndedTurn()
        {
            GameState s = m_gameHelper.GetState();
            return s.CurrentTurnState.HasEndedTurn;
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

            // First of all, we have to get a dice roll.
            if(!HasCommittedToDiceResult())
            {
                // If they haven't rolled yet, that's the only option.
                if (s.CurrentTurnState.Rolls == 0)
                {
                    actions.Add(GameActionType.RollDice);
                }
                else
                {
                    // They have rolled at least once, but they might be able to roll again.
                    if (CanRollOrReRoll())
                    {
                        actions.Add(GameActionType.RollDice);
                    }

                    // And they always have the option to commit.
                    actions.Add(GameActionType.CommitDiceResult);
                }

                // There is nothing else the player can do until they commit the roll.
                return actions;
            }

            //
            // The dice roll has been committed.
            //

            // If they haven't build yet...
            if(!HasBuildABuiding())
            {
                // ... check if there are building they can afford in the marketplace currently.
                if(m_gameHelper.Player.AreMarketplaceBuildableBuildingsAvailableThatCanAfford())
                {
                    // If so give them the option.
                    actions.Add(GameActionType.BuildBuilding);
                }
            }

            // If they haven't ended their turn, give them the option.
            if(!HasEndedTurn())
            {
                //actions.Add(GameActionType.BuildBuilding);
            }
            return actions;
        }

        public bool CanTakeAction(GameActionType type)
        {
            return GetPossibleActions().Contains(type);
        }
    }
}
