using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    // Terminology 

    public enum EstablishmentColor
    {
        Blue,
        Green,
        Red,
        Purple
    }

    public enum EstablishmentProduction
    {
        None,
        Wheat
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

        // Gets the max number of this type that are allowed in the game.
        // THIS VALUE NEEDS TO INCLUDE STARTING BUILDINGS.
        public abstract int GetMaxBuildingCountInGame();

        // Gets the max number of buildings of this type a player can have.
        public abstract int GetMaxBuildingCountPerPlayer();

        // Returns what this building produces.
        public abstract EstablishmentProduction GetEstablishmentProduction();

        // Returns the color of establishment this is.
        public abstract EstablishmentColor GetEstablishmentColor();

        // Returns if this is one of the default buildings.
        public abstract bool IsStartingBuilding();

        // Given a dice roll, this returns how many coins the player gets on their turn.
        public abstract int GetCoinsOnMyTurn(int diceValue);

        // Given a dice roll, this returns how many coins the player gets on anyone's turn.
        public abstract int GetCoinsAnyonesTurn(int diceValue);

        // Helper function, returns if the dice roll is in activation range of this
        // card or not.
        public bool IsDiceInRange(int diceValue)
        {
            (int min, int max) = GetActivationRange();
            return diceValue >= min && diceValue <= max;
        }
    }
}
