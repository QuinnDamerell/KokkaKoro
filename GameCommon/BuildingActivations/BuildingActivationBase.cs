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
        // Gets the action the player needs to decide upon to finish the activation.
        public abstract GameActionType? GetAction();

        // Called when the player makes an action for the activation.
        public abstract void PlayerAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper);

        // Called to make the activation happen.
        public abstract void Activate(List<GameLog> log, GameState state, StateHelper stateHelper, int buildingIndex, int playerIndexInvokedOn);

        // Gets some basic details for the activations.
        internal (GamePlayer, BuildingBase) GetDetailsAndValidate(GameState state, StateHelper stateHelper, string ActivationName, int buildingIndex, int playerIndexInvokedOn)
        {
            if(state == null)
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with a null state!", false, true);
            }
            if (stateHelper == null)
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with a null state helper!", false, true);
            }
            if (!stateHelper.Player.ValidatePlayerIndex(playerIndexInvokedOn))
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with an invalid playerIndex {playerIndexInvokedOn}", false, true);
            }
            if (!stateHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                throw GameErrorException.Create(state, ErrorTypes.InvalidState, $"{ActivationName} was invoked with an invalid buildilngIndex {buildingIndex}", false, true);
            }

            // Apply the coin amount to the player this card was invoked on.
            GamePlayer p = state.Players[playerIndexInvokedOn];
            BuildingBase b = stateHelper.BuildingRules[buildingIndex];
            return (p, b);
        }

        // Returns a building activation that matches a given action type.
        // If none match, returns null;
        public static BuildingActivationBase GetActivation(GameActionType type)
        {
            // This is a list of activations that have player actions.
            List<BuildingActivationBase> activations = new List<BuildingActivationBase>()
            {
                new BusinessCenterCardActivation(),
                new TvStationCardActivation()
            };

            foreach(BuildingActivationBase act in activations)
            {
                GameActionType? t = act.GetAction();
                if(t == type)
                {
                    return act;
                }
            }
            return null;
        }
    }
}
