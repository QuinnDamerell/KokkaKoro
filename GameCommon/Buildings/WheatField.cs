using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class WheatField : BuildingBase
    {
        public override string GetName()
        {
            return "WheatField";
        }

        public override string GetRule()
        {
            return "Get 1 coin from the bank. (anyone's turn)";
        }

        public override (int, int) GetActivationRange()
        {
            return (1, 1);
        }

        public override int GetCoinsAnyonesTurn(int diceValue)
        {
            if (IsDiceInRange(diceValue))
            {
                return 1;
            }
            return 0;
        }

        public override int GetCoinsOnMyTurn(int diceValue)
        {
            return 0;
        }

      
    }
}
