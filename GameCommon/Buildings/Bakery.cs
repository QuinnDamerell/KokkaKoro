using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class Bakery : BuildingBase
    {
        internal Bakery()
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "Bakery";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Get 1 coin from the bank. (your turn only)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (2, 3);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 1;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountInGame()
        {
            return 7;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountPerPlayer()
        {
            return -1;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentProduction GetEstablishmentProduction()
        {
            return EstablishmentProduction.Bread;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentColor GetEstablishmentColor()
        {
            return EstablishmentColor.Green;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool IsStartingBuilding()
        {
            return true;
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetCoinsOnMyTurn()
        {
            return 1;
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetCoinsAnyonesTurn()
        {
            return 0;
        }

        // See comments in base class. (BuildingBase.cs)
        public override List<ProductionBenefit> GetProductionBenfits()
        {
            return null;
        }
    }
}
