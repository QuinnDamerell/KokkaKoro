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
            GameState s = m_gameHelper.GetState();
            if(s.Market == null)
            {
                return "Market it null";
            }
            if(s.Market.MaxBuldingTypes < 1)
            {
                return "Market max building is too low.";
            }
            if(s.Market.AvailableBuildable == null)
            {
                return "Market's AvailableBuildable is null";
            }
            if(s.Market.AvailableBuildable.Count != m_gameHelper.BuildingRules.GetCountOfUniqueTypes())
            {
                return "Market's AvailableBuildable is smaller than building rules.";
            }
            foreach(int i in s.Market.AvailableBuildable)
            {
                if(i < 0)
                {
                    return $"Market's building index {i} has a negative value";
                }
            }
            for(int i = 0; i < m_gameHelper.BuildingRules.GetCountOfUniqueTypes(); i++)
            {
                if(m_gameHelper.BuildingRules[i].GetBuldingIndex() != i)
                {
                    return $"Building rules index {i} has an incorrect GetBuldingIndex()";
                }
            }
            if(m_gameHelper.BuildingRules.GetCountOfUniqueTypes() < 14
                || !(m_gameHelper.BuildingRules[BuildingRules.WheatField] is WheatField)
                || !(m_gameHelper.BuildingRules[BuildingRules.Ranch] is Ranch)
                || !(m_gameHelper.BuildingRules[BuildingRules.Bakery] is Bakery)
                || !(m_gameHelper.BuildingRules[BuildingRules.ConvenienceStore] is ConvenienceStore)
                || !(m_gameHelper.BuildingRules[BuildingRules.Forest] is Forest)
                || !(m_gameHelper.BuildingRules[BuildingRules.CheeseFactory] is CheeseFactory)
                || !(m_gameHelper.BuildingRules[BuildingRules.FurnitureFactory] is FurnitureFactory)
                || !(m_gameHelper.BuildingRules[BuildingRules.Mine] is Mine)
                || !(m_gameHelper.BuildingRules[BuildingRules.AppleOrchard] is AppleOrchard)
                || !(m_gameHelper.BuildingRules[BuildingRules.FarmersMarket] is FarmersMarket)
                || !(m_gameHelper.BuildingRules[BuildingRules.TrainStation] is TrainStation)
                || !(m_gameHelper.BuildingRules[BuildingRules.ShoppingMall] is ShoppingMall)
                || !(m_gameHelper.BuildingRules[BuildingRules.RadioTower] is RadioTower)
                || !(m_gameHelper.BuildingRules[BuildingRules.AmusementPark] is AmusementPark))
            {
                return $"Building rules mismatch between building constants and object type.";
            }
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
        public int GetMaxBuildableBuildingsInGame(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            GameState s = m_gameHelper.GetState();
            if (b == null)
            {
                return -1;
            }

            // Get the max building allowed to be built in any game.
            int count = m_gameHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountInGame();
            // Then if this is a starting building, add the number of players since they all have one.
            count += b.IsStartingBuilding() ? s.Players.Count : 0;
            return count;
        }

        // Returns the max number of buildable buildings given a building index that can be built per player in a given game.
        public int GetMaxBuildableBuildingsPerPlayer(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            GameState s = m_gameHelper.GetState();
            if (b == null)
            {
                return -1;
            }

            // Get the max building allowed to be build by a player.
            int count = m_gameHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountPerPlayer();

            // If negative one, it means it's the same as the max buildable in the game.
            if(count == -1)
            {
                // Note we want to buildable value here excluding starting building modifiers since that dependent on player numbers.
                count = m_gameHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountInGame();

                // But if it's a starting building, than the per player limit is the max count + 1.
                if(b.IsStartingBuilding())
                {
                    count++;
                }
            }
            return count;
        }

        // Returns the current count of built buildings by players in the current game.
        public int GetBuiltBuildingsInCurrentGame(int buildingIndex)
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

        // Returns how many buildings of the given type are still buildable by players in the game.
        public int GetBuildableInCurrentGame(int buildingIndex)
        {
            return GetMaxBuildableBuildingsInGame(buildingIndex) - GetBuiltBuildingsInCurrentGame(buildingIndex);
        }

        // Returns how many buildings of the given type are buildable by players in the marketplace.
        public int GetBuildableFromMarketplace(int buildingIndex)
        {
            if(!ValidateBuildingIndex(buildingIndex))
            {
                return -1;
            }
            GameState s = m_gameHelper.GetState();
            return s.Market.AvailableBuildable[buildingIndex];
        }

        // Returns if a a building index is currently buildable from the marketplace.
        public bool IsBuildableInCurrentGame(int buildingIndex)
        {
            return GetBuildableInCurrentGame(buildingIndex) > 0;
        }

        // Returns if a a building index is currently buildable from the marketplace.
        public bool IsBuildableFromMarketplace(int buildingIndex)
        {
            return GetBuildableFromMarketplace(buildingIndex) > 0;
        }   

        // Returns the number of unique building types (bakery, wheat field, etc) that are still available to be built
        // by players in the game.
        public int GetCountOfBuildingTypesBuildableInCurrentGame()
        {
            return GetBuildingTypesBuildableInCurrentGame().Count;
        }

        // Returns the number of building types are available to build in the marketplace.
        public int GetCountOfBuildingTypesStillBuildableInMarketplace()
        {
            return GetBuildingTypesBuildableInMarketplace().Count;
        }

        // Returns a list of building indexes that are still buildable in the game.
        public List<int> GetBuildingTypesBuildableInCurrentGame()
        {
            List<int> buildings = new List<int>();
            for (int b = 0; b < m_gameHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                if (GetBuildableInCurrentGame(b) > 0)
                {
                    buildings.Add(b);
                }
            }
            return buildings;
        }

        // Returns a list of building indexes that are current buildable from the marketplace.
        public List<int> GetBuildingTypesBuildableInMarketplace()
        {
            List<int> buildings = new List<int>();
            for (int b = 0; b < m_gameHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                if (GetBuildableFromMarketplace(b) > 0)
                {
                    buildings.Add(b);
                }
            }
            return buildings;
        }

        private BuildingBase GetBuildingRules(int buildingIndex)
        {
            if (!ValidateBuildingIndex(buildingIndex))
            {
                return null;
            }
            return m_gameHelper.BuildingRules[buildingIndex];
        }
    }
}
