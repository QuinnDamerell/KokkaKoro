using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class PlayerHelper
    {
        StateHelper m_gameHelper;

        internal PlayerHelper(StateHelper gameHelper)
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
            foreach (GamePlayer p in s.Players)
            {
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
                foreach(int i in p.OwnedBuildings)
                {
                    if(i < 0)
                    {
                        return $"Player {p.UserName}'s building count for building {i} is < 0";
                    }
                }
            }
            return null;
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

            if (!ValidateUserIndex(index))
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

            if (!ValidateUserIndex(index))
            {
                return null;
            }

            GameState s = m_gameHelper.GetState();
            return s.Players[index].Name;
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
            return 1;
        }

        public int GetMaxDiceCountCanRoll(string userName = null)
        {
            return 1;
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

        public bool ValidateUserIndex(int index = -1)
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
        public (int?, GamePlayer) CheckForWinner()
        {
            // todo
            // for now when everything is bought. the game is over.
            if(m_gameHelper.Marketplace.GetBuildingTypesBuildableInCurrentGame().Count == 0)
            {
                return (m_gameHelper.Player.GetPlayerIndex(), m_gameHelper.Player.GetPlayer());
            }
            return (null, null);
        }

        public int GetIncomeOnMyTurn(int buildingIndex, string userName = null)
        {
            if(!m_gameHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return 0;
            }
            GamePlayer p = GetPlayer(userName);
            if(p == null)
            {
                return 0;
            }

            // If they don't own it, they don't get anything.
            if(p.OwnedBuildings[buildingIndex] == 0)
            {
                return 0;
            }

            // Get the number of coins we get per building.
            int coinsPerEstablishment = m_gameHelper.BuildingRules[buildingIndex].GetCoinsOnMyTurn();
            int coinsTotal = coinsPerEstablishment * p.OwnedBuildings[buildingIndex];
            return coinsTotal;
        }

        public int GetIncomeOnAnyonesTurn(int buildingIndex, string userName = null)
        {
            if (!m_gameHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return 0;
            }
            GamePlayer p = GetPlayer(userName);
            if (p == null)
            {
                return 0;
            }

            // If they don't own it, they don't get anything.
            if (p.OwnedBuildings[buildingIndex] == 0)
            {
                return 0;
            }

            // Get the number of coins we get per building.
            int coinsPerEstablishment = m_gameHelper.BuildingRules[buildingIndex].GetCoinsAnyonesTurn();
            int coinsTotal = coinsPerEstablishment * p.OwnedBuildings[buildingIndex];
            return coinsTotal;
        }
    }
}

