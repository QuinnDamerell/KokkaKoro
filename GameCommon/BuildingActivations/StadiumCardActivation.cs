﻿using System;
using System.Collections.Generic;
using System.Text;
using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;

namespace GameCommon.BuildingActivations
{
    public class StadiumCardActivation : BuildingActivationBase
    {
        int m_amount = 2;

        public int GetAmount()
        {
            return m_amount;
        }

        public override BuildingActivationBase Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer invokedPlayer, BuildingBase b) = GetDetailsAndValidate(state, stateHelper, "StadiumCard", buildingIndex, playerIndexInvokedOn);

            // We need to take a max of 2 coins from every player but ourselves. If the player doesn't have 2 coins, take as many as they have.
            int totalTaken = 0;
            foreach(GamePlayer p in state.Players)
            {
                // Don't take coins from ourselves.
                if(p.PlayerIndex == invokedPlayer.PlayerIndex)
                {
                    continue;
                }
                int amountTaken = 0;
                if (p.Coins >= m_amount)
                {
                    amountTaken = m_amount;
                }
                else
                {
                    amountTaken = p.Coins;
                }
                p.Coins -= amountTaken;
                totalTaken += amountTaken;
            }

            // Give them to the invoked player
            invokedPlayer.Coins += totalTaken;

            // Log it
            log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.StadiumCollection, $"{invokedPlayer.Name} earned {m_amount} coins from all players (sum {totalTaken}) for a stadium action.",
                        new StadiumCollectionDetails() { TotalRecieved = totalTaken, PlayerIndexPaidTo = playerIndexInvokedOn, MaxTakenFromEachPlayer = m_amount }));                 

            return null;
        }
    }
}