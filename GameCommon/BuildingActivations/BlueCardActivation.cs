using System;
using System.Collections.Generic;
using System.Text;
using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;

namespace GameCommon.BuildingActivations
{
    public class BlueCardActivation : BuildingActivationBase
    {
        readonly int m_amount;

        public BlueCardActivation(int amount)
        {
            m_amount = amount;
        }

        public int GetAmount()
        {
            return m_amount;
        }

        public override void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer p, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "BlueCard", buildingIndex, playerIndexInvokedOn);

            // Add the coins to the player.
            p.Coins += m_amount;

            // Log it
            log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.EarnIncome, $"{p.Name} earned {m_amount} from a {b.GetName()}",
                        new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = m_amount, PlayerIndex = playerIndexInvokedOn }));                 
        }

        public override GameActionType? GetAction()
        {
            return null;
        }

        public override void PlayerAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidState, "This activation doesn't need player actions.", false, true);
        }
    }
}
