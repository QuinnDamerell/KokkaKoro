﻿using System;
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
        int m_amount;

        public RedCardActivation(int amount)
        {
            m_amount = amount;
        }

        public int GetAmount()
        {
            return m_amount;
        }

        public override BuildingActivationBase Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer invokedPlayer, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "RedCard", buildingIndex, playerIndexInvokedOn);

            // We need to take the coins from the active player and give them to the invoked player. 
            // We will take a max of the owed amount, but if there aren't enough the invoked player doesn't get the difference.
            GamePlayer activePlayer = state.Players[state.CurrentTurnState.PlayerIndex];
            int amountToTake = 0;
            if (activePlayer.Coins >= m_amount)
            {
                amountToTake = m_amount;
            }
            else
            {
                amountToTake = activePlayer.Coins;
            }

            // Take the coins from the active player
            activePlayer.Coins -= amountToTake;
            // Give them to the invoked player
            invokedPlayer.Coins += amountToTake;

            // Log it
            log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.CoinPayment, $"{activePlayer.Name} paid {amountToTake} coins to {invokedPlayer.Name} for some yummy food from a {b.GetName()}",
                        new CoinPaymentDetials() { BuildingIndex = buildingIndex, Payment = amountToTake, PlayerIndexPaidTo = playerIndexInvokedOn, PlayerIndexTakenFrom = activePlayer.PlayerIndex }));                 

            return null;
        }
    }
}