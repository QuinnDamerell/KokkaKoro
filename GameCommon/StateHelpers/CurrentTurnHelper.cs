using GameCommon.BuildingActivations;
using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    /// <summary>
    /// Helper functions to answer state questions.
    /// </summary>
    public class CurrentTurnHelper
    {
        readonly StateHelper m_baseHelper;

        internal CurrentTurnHelper(StateHelper gameHelper)
        {
            m_baseHelper = gameHelper;
        }

        /// <summary>
        /// Validates everything is correct.
        /// </summary>
        /// <returns>If there's an error, returns a string.</returns>
        public string Validate()
        {
            GameState s = m_baseHelper.GetState();
            if (s.CurrentTurnState.PlayerIndex < 0 || s.CurrentTurnState.PlayerIndex >= s.Players.Count)
            {
                return "Current turn state player index is out of bounds.";
            }
            if (s.CurrentTurnState.Rolls < 0 || s.CurrentTurnState.Rolls > m_baseHelper.Player.GetMaxRollsAllowed())
            {
                return "The current turn state rolls is to high or low for the current player.";
            }
            if(s.CurrentTurnState.Rolls > 0 && s.CurrentTurnState.DiceResults.Count == 0)
            {
                return "The current turn has rolls but no dice value.";
            }
            if(s.CurrentTurnState.DiceResults.Count > m_baseHelper.Player.GetMaxCountOfDiceCanRoll())
            {
                return "There are more dice results than then player can currently roll dice.";
            }
            if(s.CurrentTurnState.RoundNumber < 0)
            {
                return "The round number was smaller than 0";
            }
            if(s.CurrentTurnState.SpecialActions == null)
            {
                return "Current turn state activations is null";
            }
            if (s.CurrentTurnState.SpecialActions.Count > 0)
            {
                if(!s.CurrentTurnState.HasCommitedDiceResult)
                {
                    return "There are activation but the dice result hasn't been committed.";
                }
                if(s.CurrentTurnState.HasBougthBuilding)
                {
                    return "There are activations but a building has also been built.";
                }
                if(s.CurrentTurnState.HasEndedTurn)
                {
                    return "There are activations but the turn has ended.";
                }
            }
            return null;
        }


        public bool IsMyTurn(int? playerIndex = null)
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.PlayerIndex == m_baseHelper.Player.GetPlayer(playerIndex).PlayerIndex;
        }

        public bool CanRollOrReRoll()
        {
            GameState s = m_baseHelper.GetState();
            return !HasEndedTurn() && !HasCommittedToDiceResult() && s.CurrentTurnState.Rolls < m_baseHelper.Player.GetMaxRollsAllowed();
        }

        public bool HasBuildABuiding()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.HasBougthBuilding;
        }

        public bool HasCommittedToDiceResult()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.HasCommitedDiceResult;
        }

        public bool HasEndedTurn()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.HasEndedTurn;
        }

        public string GetActiveTurnPlayerUserName()
        {
            GameState s = m_baseHelper.GetState();
            return m_baseHelper.Player.GetPlayerUserName(s.CurrentTurnState.PlayerIndex);
        }

        public bool HasGameEnded()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.HasGameEnded;
        }

        public bool HasGameStarted()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.HasGameStarted;
        }

        public bool HasPendingSpecialActions()
        {
            GameState s = m_baseHelper.GetState();
            return s.CurrentTurnState.SpecialActions.Count > 0;
        }

        public List<GameActionType> GetPossibleActions()
        {
            List<GameActionType> actions = new List<GameActionType>();
            GameState s = m_baseHelper.GetState();

            // If there are any special actions for the current player, they must be resolved first.
            // If there are multiple, we need to resolve them in the order they are listed.
            if(HasPendingSpecialActions())
            {
                actions.Add(s.CurrentTurnState.SpecialActions[0]);
                return actions;
            }

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
                if(m_baseHelper.Player.AreAvailableBuildingsThatWeCanAfford())
                {
                    // If so give them the option.
                    actions.Add(GameActionType.BuildBuilding);
                }
            }

            // If they haven't ended their turn, give them the option.
            if(!HasEndedTurn())
            {
                actions.Add(GameActionType.EndTurn);
            }
            return actions;
        }

        public bool CanTakeAction(GameActionType type)
        {
            return type == GameActionType.Forfeit || GetPossibleActions().Contains(type);
        }
    }
}
