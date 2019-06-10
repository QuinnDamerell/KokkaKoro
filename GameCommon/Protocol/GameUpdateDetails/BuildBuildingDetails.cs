using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    // Indicates a building has been built.
    public class BuildBuildingDetails
    {
        // Who the building was built for.
        public int PlayerIndex;

        // The building that was built.
        public int BuildingIndex;
    }
}
