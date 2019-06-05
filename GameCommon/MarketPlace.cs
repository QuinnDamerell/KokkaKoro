using GameCommon.StateHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public class Marketplace
    {
        // The max amount of building in the market. (for now 100 will put them all in there)
        public int MaxBuldings = 100;

        // A list of buildings where the index is the building type and the value in the list is the quantity available.
        public List<int> AvailableBuilding = new List<int>();

        //
        // Helpers
        //
        public static Marketplace Create(BuildingList buildingList)
        {
            Marketplace m = new Marketplace();
            // For each building, create an entry for it in our list.
            for(int i = 0; i < buildingList.GetCount(); i++)
            {
                m.AvailableBuilding.Add(0);
            }
            return m;
        }

        public void ReplenishMarket(IRandomGenerator randomGen, StateHelper stateHelper)
        {

        }
    }
}

