using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class Ranch : BuildingBase
    {
        internal Ranch()
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "Ranch";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Get 1 coin from the bank. (anyone's turn)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (2, 2);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 1;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountInGame()
        {
            return 6;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountPerPlayer()
        {
            return -1;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentProduction GetEstablishmentProduction()
        {
            return EstablishmentProduction.Cattle;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentColor GetEstablishmentColor()
        {
            return EstablishmentColor.Blue;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool IsStartingBuilding()
        {
            return false;
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetCoinsOnMyTurn()
        {
            return 0;
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetCoinsAnyonesTurn()
        {
            return 1;
        }

        // See comments in base class. (BuildingBase.cs)
        public override List<ProductionBenefit> GetProductionBenfits()
        {
            return null;
        }
    }
}
