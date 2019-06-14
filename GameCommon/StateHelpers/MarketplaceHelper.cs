using GameCommon.Buildings;
using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class MarketplaceHelper
    {
        readonly StateHelper m_baseHelper;

        internal MarketplaceHelper(StateHelper gameHelper)
        {
            m_baseHelper = gameHelper;
        }

        /// <summary>
        /// Validates the marketplace to make sure there are no errors.
        /// </summary>
        /// <returns>If errors are found, returns a string.</returns>
        public string Validate()
        {
            GameState s = m_baseHelper.GetState();
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
            if(s.Market.AvailableBuildable.Count != m_baseHelper.BuildingRules.GetCountOfUniqueTypes())
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
            for(int i = 0; i < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); i++)
            {
                BuildingBase b = m_baseHelper.BuildingRules[i];
                if(b.GetBuildingIndex() != i)
                {
                    return $"Building rules index {i} has an incorrect GetBuldingIndex()";
                }
            }
            if(m_baseHelper.BuildingRules.GetCountOfUniqueTypes() < 14
                || !(m_baseHelper.BuildingRules[BuildingRules.WheatField] is WheatField)
                || !(m_baseHelper.BuildingRules[BuildingRules.Ranch] is Ranch)
                || !(m_baseHelper.BuildingRules[BuildingRules.Bakery] is Bakery)
                || !(m_baseHelper.BuildingRules[BuildingRules.ConvenienceStore] is ConvenienceStore)
                || !(m_baseHelper.BuildingRules[BuildingRules.Forest] is Forest)
                || !(m_baseHelper.BuildingRules[BuildingRules.CheeseFactory] is CheeseFactory)
                || !(m_baseHelper.BuildingRules[BuildingRules.FurnitureFactory] is FurnitureFactory)
                || !(m_baseHelper.BuildingRules[BuildingRules.Mine] is Mine)
                || !(m_baseHelper.BuildingRules[BuildingRules.AppleOrchard] is AppleOrchard)
                || !(m_baseHelper.BuildingRules[BuildingRules.FarmersMarket] is FarmersMarket)
                || !(m_baseHelper.BuildingRules[BuildingRules.TrainStation] is TrainStation)
                || !(m_baseHelper.BuildingRules[BuildingRules.ShoppingMall] is ShoppingMall)
                || !(m_baseHelper.BuildingRules[BuildingRules.RadioTower] is RadioTower)
                || !(m_baseHelper.BuildingRules[BuildingRules.AmusementPark] is AmusementPark))
            {
                return $"Building rules mismatch between building constants and object type.";
            }
            return null;
        }

        /// <summary>
        /// Validates the given building index.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public bool ValidateBuildingIndex(int buildingIndex)
        {
            if (buildingIndex < 0 || buildingIndex >= m_baseHelper.BuildingRules.GetCountOfUniqueTypes())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the max number of buildings that can be built in the game given the building type.
        /// The value includes the correct number of starting buildings for buildings that are starting buildings.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public int GetMaxBuildingsInGame(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            if (b == null)
            {
                return -1;
            }

            GameState s = m_baseHelper.GetState();

            // Get the max building allowed to be built in any game.
            int count = m_baseHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountInGame();

            // Then if this is a starting building, add the number of players since they all have one.
            count += b.IsStartingBuilding() ? s.Players.Count : 0;
            return count;
        }

        /// <summary>
        /// Returns the max number of buildings that can be build per player.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public int GetMaxBuildingsPerPlayer(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            if (b == null)
            {
                return -1;
            }

            // Get the max building allowed to be build by a player.
            int count = m_baseHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountPerPlayer();

            // If negative one, it means it's the same as the max buildable in the game.
            if(count == -1)
            {
                // Note we want to buildable value here excluding starting building modifiers since that dependent on player numbers.
                count = m_baseHelper.BuildingRules[buildingIndex].InternalGetMaxBuildingCountInGame();

                // But if it's a starting building, than the per player limit is the max count + number of players.
                if(b.IsStartingBuilding())
                {
                    count+= m_baseHelper.Player.GetPlayerCount();
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the current count of built buildings by players in the current game.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public int GetBuiltBuildingsInCurrentGame(int buildingIndex)
        {
            BuildingBase b = GetBuildingRules(buildingIndex);
            if (b == null)
            {
                return -1;
            }

            //  Total the number of buildings each player has.
            int sum = 0;
            GameState s = m_baseHelper.GetState();
            foreach (GamePlayer p in s.Players)
            {
                sum += p.OwnedBuildings[buildingIndex];
            }
            return sum;
        }

        /// <summary>
        /// Returns how many buildings of the given type are still buildable by players in the game.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public int GetBuildableInCurrentGame(int buildingIndex)
        {
            return GetMaxBuildingsInGame(buildingIndex) - GetBuiltBuildingsInCurrentGame(buildingIndex);
        }

        /// <summary>
        /// Returns how many buildings of the given type are buildable in the marketplace.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public int GetBuildableFromMarketplace(int buildingIndex)
        {
            if(!ValidateBuildingIndex(buildingIndex))
            {
                return -1;
            }
            GameState s = m_baseHelper.GetState();
            return s.Market.AvailableBuildable[buildingIndex];
        }

        /// <summary>
        /// Returns if a a building index is still buildable in the current game.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public bool IsBuildableInCurrentGame(int buildingIndex)
        {
            return GetBuildableInCurrentGame(buildingIndex) > 0;
        }

        /// <summary>
        /// Returns if a a building index is currently buildable from the marketplace.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <returns></returns>
        public bool IsBuildableFromMarketplace(int buildingIndex)
        {
            return GetBuildableFromMarketplace(buildingIndex) > 0;
        }

        /// <summary>
        /// Returns the number of unique building types (bakery, wheat field, etc) that are still available to be built
        /// by players in the game.
        /// </summary>
        /// <returns></returns>
        public int GetCountOfBuildingTypesBuildableInCurrentGame()
        {
            return GetBuildingTypesBuildableInCurrentGame().Count;
        }

        /// <summary>
        /// Returns the number of building types are available to build in the marketplace.
        /// </summary>
        /// <returns></returns>
        public int GetCountOfBuildingTypesStillBuildableInMarketplace()
        {
            return GetBuildingTypesBuildableInMarketplace().Count;
        }

        /// <summary>
        /// Returns a list of building indexes that are still buildable in the game.
        /// </summary>
        /// <returns></returns>
        public List<int> GetBuildingTypesBuildableInCurrentGame()
        {
            List<int> buildings = new List<int>();
            for (int b = 0; b < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                if (GetBuildableInCurrentGame(b) > 0)
                {
                    buildings.Add(b);
                }
            }
            return buildings;
        }

        /// <summary>
        /// Returns a list of building indexes that are current buildable from the marketplace.
        /// </summary>
        /// <returns></returns>
        public List<int> GetBuildingTypesBuildableInMarketplace()
        {
            List<int> buildings = new List<int>();
            for (int b = 0; b < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
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
            return m_baseHelper.BuildingRules[buildingIndex];
        }
    }
}
