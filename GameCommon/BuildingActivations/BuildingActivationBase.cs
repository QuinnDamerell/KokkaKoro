using GameCommon.Buildings;
using GameCommon.Protocol;
using GameCommon.StateHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.BuildingActivations
{
    public abstract class BuildingActivationBase
    {
        // Called th make the activation happen.
        public abstract BuildingActivationBase Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn);

        // Gets some basic details for the activations.
        internal (GamePlayer, BuildingBase) GetDetailsAndValidate(GameState state, StateHelper stateHelper, string ActivationName, int buildingIndex, int playerIndexInvokedOn)
        {
            if(state == null)
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with a null state!", false);
            }
            if (stateHelper == null)
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with a null state helper!", false);
            }
            if (!stateHelper.Player.ValidatePlayerIndex(playerIndexInvokedOn))
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with an invalid playerIndex {playerIndexInvokedOn}", false);
            }
            if (!stateHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                throw GameError.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with an invalid buildilngIndex {buildingIndex}", false);
            }

            // Apply the coin amount to the player this card was invoked on.
            GamePlayer p = state.Players[playerIndexInvokedOn];
            BuildingBase b = stateHelper.BuildingRules[buildingIndex];
            return (p, b);
        }
    }
}
