﻿using System;
using System.Collections.Generic;
using System.Text;
using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.Protocol.ActionOptions;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;
using Newtonsoft.Json.Linq;

namespace GameCommon.BuildingActivations
{
    public class TvStationCardActivation : BuildingActivationBase
    {
        int m_amount = 5;

        public int GetAmount()
        {
            return m_amount;
        }

        public override GameActionType? GetAction()
        {
            return GameActionType.TvStationPayout;
        }

        public override void PlayerAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // Try to get the options
            TvStationPayoutOptions options = null;
            try
            {
                if (action.Options is JObject obj)
                {
                    options = obj.ToObject<TvStationPayoutOptions>();
                }
            }
            catch (Exception e)
            {
                throw GameError.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"Failed to parse tv station options: {e.Message}", true);
            }
            if (!stateHelper.Player.ValidatePlayerIndex(options.PlayerIndexToTakeFrom))
            {
                throw GameError.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"Invalid player index sent in options.", true);
            }

            // Get players
            GamePlayer activePlayer = stateHelper.Player.GetPlayer();
            GamePlayer sacrificePlayer = stateHelper.Player.GetPlayerFromIndex(options.PlayerIndexToTakeFrom);

            // Validate player.
            if(activePlayer.PlayerIndex == sacrificePlayer.PlayerIndex)
            {
                throw GameError.Create(stateHelper.GetState(), ErrorTypes.ActionCantBeTakenOnSelf, $"You can't apply.", true);
            }

            // Get how many we can take, up to the max.
            int takeable = stateHelper.Player.GetMaxTakeableCoins(m_amount, sacrificePlayer.PlayerIndex);

            // Transfer the coins
            sacrificePlayer.Coins -= takeable;
            activePlayer.Coins += takeable;

            // Log the transaction.
            log.Add(GameLog.CreateGameStateUpdate(stateHelper.GetState(), StateUpdateType.CoinPayment, $"{sacrificePlayer.Name} was chosen to pay {activePlayer.Name} {takeable} coins for the TV Station.",
                        new CoinPaymentDetials() { BuildingIndex = BuildingRules.TvStation, Payment = takeable, PlayerIndexPaidTo = activePlayer.PlayerIndex, PlayerIndexTakenFrom = sacrificePlayer.PlayerIndex }));
        }

        public override void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // For this activation we need player to give us input, so we don't do anything.
        }
    }
}