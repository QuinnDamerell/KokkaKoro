using GameCommon.BuildingActivations;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class BusinessCenter : BuildingBase
    {
        internal BusinessCenter(int buildingIndex)
            : base(buildingIndex)
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "Business Center";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Exchange 1 of your non major establishments for 1 an opponent owns. (your turn only)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (6, 6);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 8;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountInGame()
        {
            return int.MaxValue;
        }

        // See comments in base class. (BuildingBase.cs)
        internal override int InternalGetMaxBuildingCountPerPlayer()
        {
            return 1;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentProduction GetEstablishmentProduction()
        {
            return EstablishmentProduction.MajorEstablishment;
        }

        // See comments in base class. (BuildingBase.cs)
        public override EstablishmentColor GetEstablishmentColor()
        {
            return EstablishmentColor.Purple;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool IsStartingBuilding()
        {
            return false;
        }

        // See comments in base class. (BuildingBase.cs)
        public override bool ActivatesOnOtherPlayersTurns()
        {
            return false;
        }

        // See comments in base class. (BuildingBase.cs)
        public override BuildingActivationBase GetActivation()
        {
            return new BusinessCenterCardActivation();
        }
    }
}
