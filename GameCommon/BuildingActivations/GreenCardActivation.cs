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
        EstablishmentProduction m_production;
        GreenCardType m_type;
        int m_amount;


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

        public override BuildingActivationBase Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer p, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "GreenCard", buildingIndex, playerIndexInvokedOn);

            // Make sure this is only being used on the active player
            if(playerIndexInvokedOn != state.CurrentTurnState.PlayerIndex)
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"GreenActivation was activated on the non-active player!", false);
            }

            // Do work
            if(m_type == GreenCardType.Static)
            {
                p.Coins += m_amount;

                // Log it
                log.Add(EarnIncomeDetails.Create(state, p.Name, playerIndexInvokedOn, b.GetName(), buildingIndex, m_amount));
            }
            else if(m_type == GreenCardType.PerProduct)
            {
                int count = stateHelper.Player.GetTotalProductionTypeBuilt(m_production, playerIndexInvokedOn);
                int totalEarned = m_amount * count;
                p.Coins += totalEarned;

                log.Add(EarnIncomeDetails.Create(state, p.Name, playerIndexInvokedOn, b.GetName(), buildingIndex, totalEarned));
            }
            else
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"GreenActivation was activated with an unknown type.", false);
            }

            return null;
        }
    }
}
