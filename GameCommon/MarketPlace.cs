using GameCommon.StateHelpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public class Marketplace
    {
        // The max amount of building in the market. (for now 100 will put them all in there)
        public int MaxBuldingTypes = 100;

        // A list of buildings where the index is the building type and the value in the list is the quantity available.
        public List<int> AvailableBuildable = new List<int>();

        //
        // Helpers
        //
        public static Marketplace Create(BuildingRules buildingRules)
        {
            Marketplace m = new Marketplace();
            // For each building, create an entry for it in our list.
            for(int i = 0; i < buildingRules.GetCountOfUniqueTypes(); i++)
            {
                m.AvailableBuildable.Add(0);
            }
            return m;
        }

        public void ReplenishMarket(IRandomGenerator randomGen, StateHelper stateHelper)
        {
            // While we are under our marketplace limit for buildings and there are still more available buildings in the game
            while(true)
            {
                // First, check to see if we have all of the buildings in the game available in the marketplace.
                if(stateHelper.Marketplace.GetCountOfBuildingTypesStillBuildableInMarketplace() == stateHelper.Marketplace.GetCountOfBuildingTypesStillBuildableInGame())
                {
                    // If this is the case, we want to make sure the quantity remaining for all buildings is in the marketplace.
                    for(int i = 0; i < stateHelper.BuildingRules.GetCountOfUniqueTypes(); i++)
                    {
                        // For each building, set the amount to the total that are buildable in the game.
                        AvailableBuildable[i] = stateHelper.Marketplace.GetStillBuildableInGame(i);
                    }
                    
                    // Once we know all building are listed, we are done.
                    return;
                }

                // Now check if we are under our building type limits.
                if(stateHelper.Marketplace.GetCountOfBuildingTypesStillBuildableInMarketplace() == MaxBuldingTypes)
                {
                    // If we have the limit, we are done.
                    return;
                }

                // We need to add a building to the marketplace.
                List<int> buildableBuildings = stateHelper.Marketplace.GetBuildingTypesStillBuildableInGame();
                int randomIndex = randomGen.RandomInt(0, buildableBuildings.Count - 1);

                // Add the building. This might add a new building to the marketplace or add another build count
                // to an existing building.
                AvailableBuildable[randomIndex] = AvailableBuildable[randomIndex] + 1;
            }
        }
    }
}

