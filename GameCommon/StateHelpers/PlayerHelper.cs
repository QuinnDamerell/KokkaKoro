using GameCommon.Buildings;
using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class PlayerHelper
    {
        StateHelper m_gameHelper;
        string m_perspectiveUserName;

        internal PlayerHelper(StateHelper gameHelper, string fromPerspectiveUserName)
        {
            m_gameHelper = gameHelper;
        }

        public string Validate()
        {
            GameState s = m_gameHelper.GetState();
            if(s.Players == null)
            {
                return "Players object is null";
            }
            if(s.Players.Count == 0)
            {
                return "There are no players";
            }
            if(String.IsNullOrWhiteSpace(m_perspectiveUserName))
            {
                return "There is no perspective user name.";
            }
            int count = 0;
            bool matchedPerspective = false;
            foreach (GamePlayer p in s.Players)
            {
                if(p.PlayerIndex != count)
                {
                    return "The player index doesn't match the list index.";
                }
                count++;
                if (String.IsNullOrWhiteSpace(p.Name) || String.IsNullOrWhiteSpace(p.UserName))
                {
                    return "A user has a empty name or username.";
                }
                if (p.Coins < 0)
                {
                    return $"User {p.UserName} has less than 0 coins";
                }
                if(p.OwnedBuildings == null)
                {
                    return $"Player {p.UserName} owned buildings object is null";
                }
                if(p.OwnedBuildings.Count != m_gameHelper.BuildingRules.GetCountOfUniqueTypes())
                {
                    return $"Player {p.UserName}'s owned building list is too short";
                }
                if(p.UserName == m_perspectiveUserName)
                {
                    matchedPerspective = true;
                }
                foreach(int i in p.OwnedBuildings)
                {
                    if(i < 0)
                    {
                        return $"Player {p.UserName}'s building count for building {i} is < 0";
                    }
                }
            }
            if(!matchedPerspective)
            {
                return $"The perspective user name {m_perspectiveUserName} didn't match and player user names";
            }
            return null;
        }


        /// <summary>
        /// Returns the user name perspective of the player all default question will be answered from.
        /// </summary>
        /// <returns></returns>
        public string GetPerspectiveUserName()
        {
            return m_perspectiveUserName;
        }

        /// <summary>
        /// Sets a new user perspective. 
        /// </summary>
        /// <param name="newUserName"></param>
        public void SetPerspectiveUserName(string newUserName)
        {
            m_perspectiveUserName = newUserName;
        }

        public int GetPlayerCount()
        {
            GameState s = m_gameHelper.GetState();
            return s.Players.Count;
        }

        public int GetPlayerIndex(string userName = null)
        {
            GamePlayer p = ValidatePlayer(userName);
            if (p == null)
            {
                return -1;
            }

            GameState s = m_gameHelper.GetState();
            for (int i = 0; i < s.Players.Count; i++)
            {
                if (s.Players[i].UserName.Equals(p.UserName))
                {
                    return i;
                }
            }
            return -1;
        }

        public string GetPlayerUserName(int? i = null)
        {
            int index = i.HasValue ? i.Value : -1;
            if (!i.HasValue)
            {
                index = GetPlayerIndex();
            }

            if (!ValidatePlayerIndex(index))
            {
                return null;
            }

            GameState s = m_gameHelper.GetState();   
            return s.Players[index].UserName;
        }

        public string GetPlayerName(int? i = null)
        {
            int index = i.HasValue ? i.Value : -1;
            if (!i.HasValue)
            {
                index = GetPlayerIndex();
            }

            if (!ValidatePlayerIndex(index))
            {
                return null;
            }

            GameState s = m_gameHelper.GetState();
            return s.Players[index].Name;
        }

        public GamePlayer GetPlayerFromIndex(int? playerIndex = null)
        {
            GameState s = m_gameHelper.GetState();

            if (!playerIndex.HasValue)
            {
                int count = 0;
                foreach(GamePlayer p in s.Players)
                {
                    if(p.UserName.Equals(m_gameHelper.GetPerspectiveUserName()))
                    {
                        playerIndex = count;
                        break;
                    }
                    count++;
                }
            }
            if(!playerIndex.HasValue || !ValidatePlayerIndex(playerIndex.Value))
            {
                return null;
            }
            return s.Players[playerIndex.Value];
        }

        public GamePlayer GetPlayer(string userName = null)
        {
            GameState s = m_gameHelper.GetState();
            int index = GetPlayerIndex(userName);
            if(index == -1)
            {
                return null;
            }
            return s.Players[index];
        }

        public int GetMaxRollsAllowed(string userName = null)
        {
            GamePlayer p = GetPlayer(userName);
            if (p == null)
            {
                return -1;
            }
            // If the player owns a radio tower, they can reroll the dice once per turn if desired..
            return p.OwnedBuildings[BuildingRules.RadioTower] > 0 ? 2 : 1;
        }

        public int GetMaxDiceCountCanRoll(string userName = null)
        {
            GamePlayer p = GetPlayer(userName);
            if(p == null)
            {
                return -1;
            }
            // If the player owns a train station, they can roll two dice.
            return p.OwnedBuildings[BuildingRules.TrainStation] > 0 ? 2 : 1;
        }

        // Returns if the player can take another turn after their current turn.
        public bool GetsExtraTurn(string userName = null)
        {
            GamePlayer p = GetPlayer(userName);
            if (p == null)
            {
                return false;
            }

            // Check if they rolled doubles
            GameState s = m_gameHelper.GetState();
            if (s.CurrentTurnState.DiceResults.Count < 2)
            {
                return false;
            }           
            bool hasDoubleMatch = false;
            int outterCount = 0;
            foreach(int outer in s.CurrentTurnState.DiceResults)
            {
                int innerCount = 0;
                foreach (int inner in s.CurrentTurnState.DiceResults)
                {
                    if(outterCount != innerCount && outer == inner)
                    {
                        hasDoubleMatch = true;
                        break;
                    }
                    innerCount++;
                }
                outterCount++;
                if(hasDoubleMatch)
                {
                    break;
                }
            }
            if(!hasDoubleMatch)
            {
                return false;
            }           

            // If the player owns a amusement park, they get another turn if they roll doubles.
            return p.OwnedBuildings[BuildingRules.AmusementPark] > 0;
        }

        // Indicates if the player has a shopping mall.
        public bool HasShoppingMall(string userName = null)
        {
            GamePlayer p = GetPlayer(userName);
            if (p == null)
            {
                return false;
            }
            // If the player owns a shopping mall, they get +1 for each cup and bread establishment.
            return p.OwnedBuildings[BuildingRules.ShoppingMall] > 0;
        }

        // Returns the number of buildings this player currently has built.
        public int GetBuiltCount(int buildingIndex, int? playerIndex = null)
        {
            // Validate the building index
            if (!m_gameHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return -1;
            }

            // Get the player
            GamePlayer p = GetPlayerFromIndex(playerIndex);
            if (p == null)
            {
                return -1;
            }

            // Get the building they own.
            return p.OwnedBuildings[buildingIndex];
        }

        // Returns the number of buildings this player currently has built.
        public int GetBuiltCount(int buildingIndex, string userName = null)
        {
            // Validate the building index
            if (!m_gameHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return -1;
            }

            // Get the player
            GamePlayer p = GetPlayer(userName);
            if(p == null)
            {
                return -1;
            }

            // Get the building they own.
            return p.OwnedBuildings[buildingIndex];
        }

        // Returns if the user can build another building type, or if they have hit the limit.
        public bool HasReachedPerPlayerBuildingLimit(int buildingIndex, string userName = null)
        {
            // This will validate user name and building index.
            int built = GetBuiltCount(buildingIndex, userName);
            if (built == -1)
            {
                return true;
            }
            if (built >= m_gameHelper.Marketplace.GetMaxBuildableBuildingsPerPlayer(buildingIndex))
            {
                return true;
            }
            return false;
        }

        public bool CanAffordBuilding(int buildingIndex, string userName = null)
        {
            // Validate the building index
            if (!m_gameHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return false;
            }

            GamePlayer p = GetPlayer(userName);
            if(p == null)
            {
                return false;
            }

            // Check if they have the coins to build it.
            if (p.Coins < m_gameHelper.BuildingRules[buildingIndex].GetBuildCost())
            {
                return false;
            }

            return true;
        }

        public int GetMaxTakeableCoins(int desiredAmount, int? playerIndex = null)
        {
            // Get the player
            GamePlayer p = GetPlayerFromIndex(playerIndex);
            if (p == null)
            {
                return -1;
            }

            // Return the desired amount or however many coins they have.
            return Math.Min(desiredAmount, p.Coins);
        }

        public bool CanBuildBuilding(int buildingIndex, string userName = null)
        {
            // Check if we can afford it.
            // (this will validate the index and user name)
            if(!CanAffordBuilding(buildingIndex, userName))
            {
                return false;
            }         

            // Check if it's available in the marketplace.
            if(!m_gameHelper.Marketplace.IsBuildableFromMarketplace(buildingIndex))
            {
                return false;
            }

            // Last, check if the player already has the per player building limit.
            if(HasReachedPerPlayerBuildingLimit(buildingIndex, userName))
            {
                return false;
            }
            return true;
        }

        // Returns if there are any building that are buildable from the marketplace and we can afford.
        public bool AreMarketplaceBuildableBuildingsAvailableThatCanAfford(string userName = null)
        {
            // Get all building that are in the marketplace.
            List<int> buildable = m_gameHelper.Marketplace.GetBuildingTypesBuildableInMarketplace();

            // See if there are any we can afford.
            return FilterBuildingIndexsWeCanAfford(buildable, userName).Count > 0;
        }

        // Gets a list of building indexes that the player can afford and are buildable. available 
        public List<int> FilterBuildingIndexsWeCanAfford(List<int> buildingIndexes, string userName = null)
        {
            List<int> canAffordAndBuildable = new List<int>();
            GamePlayer p = GetPlayer(userName);
            if(p == null)
            {
                return canAffordAndBuildable;
            }

            foreach(int b in buildingIndexes)
            {
                // Filter out only the ones we can afford and haven't bought too many of.
                if(CanAffordBuilding(b, userName) && !HasReachedPerPlayerBuildingLimit(b, userName))
                {
                    canAffordAndBuildable.Add(b);
                }
            }
            return canAffordAndBuildable;
        }

        public bool ValidatePlayerIndex(int index = -1)
        {
            GameState s = m_gameHelper.GetState();
            if (index < 0 || index >= s.Players.Count)
            {
                return false;
            }
            return true;
        }

        public GamePlayer ValidatePlayer(string userName = null)
        {
            // Get the correct user name.
            userName = userName == null ? m_gameHelper.GetPerspectiveUserName() : userName;

            GameState s = m_gameHelper.GetState();
            foreach(GamePlayer p in s.Players)
            {
                if(p.UserName.Equals(userName))
                {
                    return p;
                }
            }
            return null;
        }

        // Returns null if no winner was found, otherwise the winning player.
        public GamePlayer CheckForWinner()
        {
            GameState s = m_gameHelper.GetState();
            foreach (GamePlayer p in s.Players)
            {
                // If they own all of the landmarks, they win!
                if (p.OwnedBuildings[BuildingRules.TrainStation] > 0
                    && p.OwnedBuildings[BuildingRules.ShoppingMall] > 0
                    && p.OwnedBuildings[BuildingRules.AmusementPark] > 0
                    && p.OwnedBuildings[BuildingRules.RadioTower] > 0)
                {
                    return p;
                }
            }
            return null;
        }

        // Returns the count of buildings built by the player that have the given production type.
        public int GetTotalProductionTypeBuilt(EstablishmentProduction production, int? playerIndex = null)
        {
            if(!playerIndex.HasValue)
            {
                playerIndex = GetPlayerIndex();
            }
            if(!ValidatePlayerIndex(playerIndex.Value))
            {
                return -1;
            }

            // Count the building count for the given production.
            GameState s = m_gameHelper.GetState();
            int total = 0;
            for (int buildingIndex = 0; buildingIndex < m_gameHelper.BuildingRules.GetCountOfUniqueTypes(); buildingIndex++)
            {
                BuildingBase b = m_gameHelper.BuildingRules[buildingIndex];
                if(b.GetEstablishmentProduction() == production)
                {
                    total += GetBuiltCount(buildingIndex, playerIndex);
                }
            }
            return total;
        }
    }
}

