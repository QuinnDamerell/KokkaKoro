using System;
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
        readonly int m_amount = 2;

        public int GetAmount()
        {
            return m_amount;
        }

        public override void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // Get common details and validate.
            (GamePlayer invokedPlayer, BuildingBase _) = GetDetailsAndValidate(state, stateHelper, "StadiumCard", buildingIndex, playerIndexInvokedOn);

            // We need to take a max of 2 coins from every player but ourselves. If the player doesn't have 2 coins, take as many as they have.
            int totalTaken = 0;
            foreach(GamePlayer p in state.Players)
            {
                // Don't take coins from ourselves.
                if(p.PlayerIndex == invokedPlayer.PlayerIndex)
                {
                    continue;
                }

                // Get how many coins we can take.
                int amountToTake = stateHelper.Player.GetMaxTakeableCoins(m_amount, p.PlayerIndex);

                // Take them.
                p.Coins -= amountToTake;
                totalTaken += amountToTake;
            }

            // Give them to the invoked player
            invokedPlayer.Coins += totalTaken;

            // Log it
            log.Add(GameLog.CreateGameStateUpdate(state, StateUpdateType.StadiumCollection, $"{invokedPlayer.Name} earned {m_amount} coins from all players (sum {totalTaken}) for a stadium action.", true,
                        new StadiumCollectionDetails() { TotalRecieved = totalTaken, PlayerIndexPaidTo = playerIndexInvokedOn, MaxTakenFromEachPlayer = m_amount }));                 
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
