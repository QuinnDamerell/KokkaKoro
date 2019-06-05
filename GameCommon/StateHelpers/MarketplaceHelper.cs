using GameCommon.Buildings;
using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class MarketplaceHelper
    {
        StateHelper m_gameHelper;

        internal MarketplaceHelper(StateHelper gameHelper)
        {
            m_gameHelper = gameHelper;
        }

        public string Validate()
        {

            return null;
        }

        // Validates that a building index is valid.
        public bool ValidateBuildingIndex(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= m_gameHelper.BuildingRules.GetCountOfUniqueTypes())
            {
                return false;
            }
            return true;
        }

        // Returns the max number of buildings of the given type that can be built in the game.
        // The value includes the correct number of starting buildings for buildings that are starting buildings.
        public int GetMaxAllowableBuiltBuildingsInGame(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            GameState s = m_gameHelper.GetState();
            if (b == null)
            {
                return -1;
            }

            // Get the max building allowed to be built in any game.
            int count = m_gameHelper.BuildingRules[buildingIndex].GetMaxBuildingCountInGame();
            // Then if this is a starting building, add the number of players since they all have one.
            count += b.IsStartingBuilding() ? s.Players.Count : 0;
            return count;
        }

        // Returns the current count of built buildings by players in the current game.
        public int GetCurrentBuiltBuildingsInGame(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            GameState s = m_gameHelper.GetState();
            if (b == null)
            {
                return -1;
            }

            //  Total the number of buildings each player has.
            int sum = 0;
            foreach(GamePlayer p in s.Players)
            {
                sum += p.OwnedBuildings[buildingIndex];
            }
            return sum;
        }

        public int GetCountOfAvailable

        // Returns the number of unique building types (bakery, wheat field, etc) that are still available in the game.
        public int GetCountOfUniqueBuildingTypesAvailableInGame()
        {
            GameState s = m_gameHelper.GetState();
            for (int b = 0; b < m_gameHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                BuildingBase building = m_gameHelper.BuildingRules[b];
                if()

            }
        }

        // Returns the number of building types are still available in the game.
        public int GetCountOfAvailableUniqueBuidingTypesInMarketplace()
        {
            int count = 0;
            GameState s = m_gameHelper.GetState();
            for(int i = 0; i < s.Market.AvailableBuilding.Count; i++)
            {
                if(s.Market.AvailableBuilding)
            }
        }

        public int GetAvailableNumberOfBuidingInMarktplace(int buildingIndex)
        {
            GameState s = m_gameHelper.GetState();
            return s.Market.AvailableBuilding[buildingIndex];
        }

        private BuildingBase GetBuildingRules(int buildingIndex)
        {
            if (ValidateBuildingIndex(buildingIndex))
            {
                return null;
            }
            return m_gameHelper.BuildingRules[buildingIndex];
        }
    }
}
