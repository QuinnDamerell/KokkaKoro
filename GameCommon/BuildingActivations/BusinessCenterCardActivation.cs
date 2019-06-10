using System;
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
    public class BusinessCenterCardActivation : BuildingActivationBase
    {
        public override GameActionType? GetAction()
        {
            return GameActionType.BusinessCenterSwap;
        }

        public override void PlayerAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // Try to get the options
            BusinessCenterSwapOptions options = null;
            try
            {
                if (action.Options is JObject obj)
                {
                    options = obj.ToObject<BusinessCenterSwapOptions>();
                }
            }
            catch (Exception e)
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"Failed to parse business center options: {e.Message}", true);
            }

            // If the player decided to skip the swap, let them.
            GamePlayer activePlayer = stateHelper.Player.GetPlayer();
            if (options.SkipAction)
            {
                // Log the transaction.
                log.Add(GameLog.CreateGameStateUpdate(stateHelper.GetState(), StateUpdateType.ActionSkip, $"{activePlayer.Name} has chosen to skip their {GameActionType.BusinessCenterSwap.ToString()}.",
                           new ActionSkipDetails() { PlayerIndex = activePlayer.PlayerIndex, SkippedAction = GameActionType.BusinessCenterSwap }));
                return;
            }

            if (!stateHelper.Player.ValidatePlayerIndex(options.PlayerIndexToSwapWith))
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"Invalid player index sent in options.", true);
            }
            if (!stateHelper.Marketplace.ValidateBuildingIndex(options.BuildingIndexToGive) || !stateHelper.Marketplace.ValidateBuildingIndex(options.BuildingIndexToTake))
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"Invalid business index sent in options.", true);
            }

            // Get the swap player.
            GamePlayer sacrificePlayer = stateHelper.Player.GetPlayerFromIndex(options.PlayerIndexToSwapWith);

            // Validate.
            if (activePlayer.PlayerIndex == sacrificePlayer.PlayerIndex)
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.ActionCantBeTakenOnSelf, $"You can't apply this action to yourself.", true);
            }

            // Get the building and validate.
            BuildingBase give = stateHelper.BuildingRules[options.BuildingIndexToGive];
            BuildingBase take = stateHelper.BuildingRules[options.BuildingIndexToTake];
            if (take.GetEstablishmentColor() == EstablishmentColor.Landmark || take.GetEstablishmentColor() == EstablishmentColor.Purple
                || give.GetEstablishmentColor() == EstablishmentColor.Landmark || give.GetEstablishmentColor() == EstablishmentColor.Purple)
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"One of the selected buildings was a purple or landmark.", true);
            }
            if (activePlayer.OwnedBuildings[give.GetBuildingIndex()] == 0)
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"You don't own a building of type {give.GetName()} to give!", true);
            }
            if(sacrificePlayer.OwnedBuildings[take.GetBuildingIndex()] == 0)
            {
                throw GameErrorException.Create(stateHelper.GetState(), ErrorTypes.InvalidActionOptions, $"The player {sacrificePlayer.Name} doesn't own the building you want to take. ({take.GetName()})", true);
            }

            // Do the swap.
            activePlayer.OwnedBuildings[give.GetBuildingIndex()]--;
            activePlayer.OwnedBuildings[take.GetBuildingIndex()]++;
            sacrificePlayer.OwnedBuildings[take.GetBuildingIndex()]--;
            sacrificePlayer.OwnedBuildings[give.GetBuildingIndex()]++;

            // Log the swap.
            log.Add(GameLog.CreateGameStateUpdate(stateHelper.GetState(), StateUpdateType.BusinessCenterSwap, $"{activePlayer.Name} chose to take {take.GetName()} from {sacrificePlayer.Name} in exchange for {give.GetName()}.",
             new BusinessCenterSwapDetails() { PlayerIndex1 = activePlayer.PlayerIndex, PlayerIndex2 = sacrificePlayer.PlayerIndex, BudingIndexPlayer1Recieved = take.GetBuildingIndex(), BudingIndexPlayer2Recieved = give.GetBuildingIndex() }));
        }

        public override void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn)
        {
            // For this activation we need player to give us input, so we don't do anything.
        }
    }
}
