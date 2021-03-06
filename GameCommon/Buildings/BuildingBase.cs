﻿using GameCommon.BuildingActivations;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    public enum EstablishmentColor
    {
        Blue,
        Green,
        Red,
        Purple,
        Landmark
    }

    public enum EstablishmentProduction
    {
        None,
        Wheat,
        Cattle,
        Bread,
        Gear,
        Cup,
        Factory,
        Fruit,
        MajorEstablishment
    }

    public class ProductionBenefit
    {
        // The type of production that benefits.
        public EstablishmentProduction Prodution;

        // The value you get per production.
        public int Value;
    }
    
    public abstract class BuildingBase
    {
        // Returns the building name.
        public abstract string GetName();

        // Returns a human readable description of the card.
        public abstract string GetRule();

        // Returns an inclusive range in which the card is activated.
        public abstract (int, int) GetActivationRange();

        // Returns how expensive the building is to build.
        public abstract int GetBuildCost();

        // Internal because this requires game state specific logic. 
        // Use StateHelper.Marketplace.GetMaxBuildableBuildingsInGame to get the value.
        //
        // Gets the max number of this type that are allowed in the game.
        // THIS VALUE EXCULDES STARTING BUILDINGS.
        internal abstract int InternalGetMaxBuildingCountInGame();

        // Internal because this requires game state specific logic. 
        // Use StateHelper.Marketplace.GetMaxBuildableBuildingsPerPlayer to get the value.
        //
        // Gets the max number of buildings of this type a player can have.
        // Returns -1 if the limit is max buildable in game.
        // THIS VALUE EXCULDES STARTING BUILDINGS.
        internal abstract int InternalGetMaxBuildingCountPerPlayer();

        // Returns what this building produces.
        public abstract EstablishmentProduction GetEstablishmentProduction();

        // Returns the color of establishment this is.
        public abstract EstablishmentColor GetEstablishmentColor();

        // Returns if this is one of the default buildings.
        public abstract bool IsStartingBuilding();

        // Indicates if the building activates on other player's turns or not.
        public abstract bool ActivatesOnOtherPlayersTurns();

        // Gets the activation action for the building.
        public abstract BuildingActivationBase GetActivation();

        int m_buildingIndex;
        public BuildingBase(int buildingIndex)
        {
            m_buildingIndex = buildingIndex;
        }

        // Returns the building index in the BuildingRules for this Building.
        public int GetBuildingIndex()
        {
            return m_buildingIndex;
        }

        // Helper function, returns if the dice roll is in activation range of this
        // card or not.
        public bool IsDiceInRange(int diceValue)
        {
            (int min, int max) = GetActivationRange();
            return diceValue >= min && diceValue <= max;
        }
    }
}
