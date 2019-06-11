using System;
using System.Collections.Generic;
using System.Text;
using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;

namespace GameCommon.BuildingActivations
{
    public class RedCardActivation : BuildingActivationBase
    {
        readonly int m_amount;

        public RedCardActivation(int amount)
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
            (GamePlayer invokedPlayer, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "RedCard", buildingIndex, playerIndexInvokedOn);

            // We need to account for the shopping mall bonus if the player qualifies
            int bonusAmount = 0;
            string bonus = String.Empty;
            if (stateHelper.Player.ShouldGetShoppingMallBonus(buildingIndex, playerIndexInvokedOn))
            {
                bonusAmount = 1;
                bonus = " (bonus +1 coin from shopping mall)";
            }

            int maxToTake = m_amount + bonusAmount;

            // We need to take the coins from the active player and give them to the invoked player. 
            // We will take a max of the owed amount, but if there aren't enough the invoked player doesn't get the difference.
            GamePlayer activePlayer = state.Players[state.CurrentTurnState.PlayerIndex];
            int amountToTake = stateHelper.Player.GetMaxTakeableCoins(maxToTake, activePlayer.PlayerIndex);

            // Take the coins from the active player
            activePlayer.Coins -= amountToTake;
            // Give them to the invoked player
            invokedPlayer.Coins += amountToTake;

            // Log it
            log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.CoinPayment, $"{activePlayer.Name} paid {amountToTake} coins to {invokedPlayer.Name} for some yummy food from a {b.GetName()}.{bonus}",
                        new CoinPaymentDetials() { BuildingIndex = buildingIndex, Payment = amountToTake, PlayerIndexPaidTo = playerIndexInvokedOn, PlayerIndexTakenFrom = activePlayer.PlayerIndex }));                 
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
