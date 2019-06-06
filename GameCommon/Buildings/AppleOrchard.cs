using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class AppleOrchard : BuildingBase
    {
        internal AppleOrchard()
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "AppleOrchard";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Get 3 coin from the bank. (anyone's turn)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (10, 10);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 3;
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
            return EstablishmentProduction.Wheat;
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
            return 3;
        }

        // See comments in base class. (BuildingBase.cs)
        public override List<ProductionBenefit> GetProductionBenfits()
        {
            return null;
        }
    }
}
