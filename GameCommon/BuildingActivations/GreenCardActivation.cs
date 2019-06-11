using System;
using System.Collections.Generic;
using System.Text;
using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;

namespace GameCommon.BuildingActivations
{
    public enum GreenCardType
    {
        Static,
        PerProduct
    }

    public class GreenCardActivation : BuildingActivationBase
    {
        readonly EstablishmentProduction m_production;
        readonly GreenCardType m_type;
        readonly int m_amount;


        public GreenCardActivation(EstablishmentProduction production, int amountPerProduction)
        {
            m_type = GreenCardType.PerProduct;
            m_amount = amountPerProduction;
            m_production = production;
        }

        public GreenCardActivation(int staticAmount)
        {
            m_type = GreenCardType.Static;
            m_amount = staticAmount;
        }

        new public GreenCardType GetType()
        {
            return m_type;
        }

        public EstablishmentProduction GetProduction()
        {
            return m_production;
        }

        public int GetAmount()
        {
            return m_amount;
        }

        public override void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer p, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "GreenCard", buildingIndex, playerIndexInvokedOn);

            // Make sure this is only being used on the active player
            if(playerIndexInvokedOn != state.CurrentTurnState.PlayerIndex)
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"GreenActivation was activated on the non-active player!", false, true);
            }
                       
            // We need to account for the shopping mall bonus if the player qualifies
            int bonusAmount = 0;
            string bonus = String.Empty;
            if (stateHelper.Player.ShouldGetShoppingMallBonus(buildingIndex, playerIndexInvokedOn))
            {
                bonusAmount = 1;
                bonus = " (bonus +1 coin from shopping mall)";
            }

            // Do work
            if (m_type == GreenCardType.Static)
            {
                int totalEarned = m_amount + bonusAmount;
                p.Coins += totalEarned;

                // Log it
                log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.EarnIncome, $"{p.Name} earned {totalEarned} from a {b.GetName()}.{bonus}",
                            new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = totalEarned, PlayerIndex = playerIndexInvokedOn }));
            }
            else if(m_type == GreenCardType.PerProduct)
            {
                int count = stateHelper.Player.GetTotalProductionTypeBuilt(m_production, playerIndexInvokedOn);
                int totalEarned = (m_amount * count) + bonusAmount;
                p.Coins += totalEarned;

                // Log it
                log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.EarnIncome, $"{p.Name} earned {totalEarned} from a {b.GetName()}.{bonus}",
                            new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = totalEarned, PlayerIndex = playerIndexInvokedOn }));
            }
            else
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"GreenActivation was activated with an unknown type.", false, true);
            }            
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
