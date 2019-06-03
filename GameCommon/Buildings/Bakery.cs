using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class Bakery : BuildingBase
    {
        public override string GetName()
        {
            return "Bakery";
        }

        public override string GetRule()
        {
            return "Get 1 coin from the bank. (your turn only)";
        }

        public override (int, int) GetActivationRange()
        {
            return (2, 3);
        }

        public override int GetCoinsAnyonesTurn(int diceValue)
        {
            return 0;
        }

        public override int GetCoinsOnMyTurn(int diceValue)
        {
            if(IsDiceInRange(diceValue))
            {
                return 1;
            }
            return 0;
        }      
    }
}
