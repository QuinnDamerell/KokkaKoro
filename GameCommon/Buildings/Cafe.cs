using GameCommon.BuildingActivations;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class Cafe : BuildingBase
    {
        internal Cafe(int buildingIndex)
            : base(buildingIndex)
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "Cafe";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Take 1 coin from the active player. (opponent's turn)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (3, 3);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 2;
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
            return EstablishmentProduction.Cup;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentColor GetEstablishmentColor()
        {
            return EstablishmentColor.Red;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool IsStartingBuilding()
        {
            return false;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool ActivatesOnOtherPlayersTurns()
        {
            return true;
        }

        // See comments in base class. (BuildingBase.cs)
        public override BuildingActivationBase GetActivation()
        {
            return new RedCardActivation(1);
        }
    }
}
