﻿using GameCommon.BuildingActivations;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Buildings
{
    class Mine : BuildingBase
    {
        internal Mine(int buildingIndex)
            : base(buildingIndex)
        { }

        // See comments in base class. (BuildingBase.cs)
        public override string GetName()
        {
            return "Mine";
        }

        // See comments in base class. (BuildingBase.cs)
        public override string GetRule()
        {
            return "Get 5 coin from the bank. (anyone's turn)";
        }

        // See comments in base class. (BuildingBase.cs)
        public override (int, int) GetActivationRange()
        {
            return (9, 9);
        }

        // See comments in base class. (BuildingBase.cs)
        public override int GetBuildCost()
        {
            return 6;
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
            return EstablishmentProduction.Gear;
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
        public override bool ActivatesOnOtherPlayersTurns()
        {
            return true;
        }

        // See comments in base class. (BuildingBase.cs)
        public override BuildingActivationBase GetActivation()
        {
            return new BlueCardActivation(5);
        }
    }
}
