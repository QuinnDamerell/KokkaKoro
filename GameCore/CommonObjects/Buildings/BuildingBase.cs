using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects.Buildings
{
    public abstract class BuildingBase
    {
        // Returns the buliding name.
        public abstract string GetName();

        // Retuns a human readable description of the card.
        public abstract string GetRule();

        // Retuns an inclusive range in which the card is activated.
        public abstract (int, int) GetActivationRange();

        // Given a dice roll, this returns how many coins the player gets on their turn.
        public abstract int GetCoinsOnMyTurn(int diceValue);

        // Given a dice roll, this returns how many coints the player gets on anyone's turn.
        public abstract int GetCoinsAnyonesTurn(int diceValue);



        // Helper funciton, returns if the dice roll is in activation range of this
        // card or not.
        public bool IsDiceInRange(int diceValue)
        {
            (int min, int max) = GetActivationRange();
            return diceValue >= min && diceValue <= max;
        }

    }
}
