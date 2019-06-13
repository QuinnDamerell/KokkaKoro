using GameCommon.Buildings;
using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.StateHelpers
{
    public class KokkaKoroLeaderboardElement
    {
        public int Rank;
        public int LandmarksOwned;
        public GamePlayer Player;        
    }

    /// <summary>
    /// Helper functions to answer player state questions.
    /// </summary>
    public class PlayerHelper
    {
        readonly StateHelper m_baseHelper;
        string m_perspectivePlayerUserName;
        int m_perspectivePlayerIndex;

        internal PlayerHelper(StateHelper gameHelper, string fromPerspectiveUserName)
        {
            m_baseHelper = gameHelper;
            SetPerspectiveUserName(fromPerspectiveUserName);
        }

        /// <summary>
        /// Ensures there are no state validations.
        /// </summary>
        /// <returns>Returns a string if there is an error.</returns>
        public string Validate()
        {
            GameState s = m_baseHelper.GetState();
            if(s.Players == null)
            {
                return "Players object is null";
            }
            if(s.Players.Count == 0)
            {
                return "There are no players";
            }
            if(String.IsNullOrWhiteSpace(m_perspectivePlayerUserName))
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
                if (String.IsNullOrWhiteSpace(p.Name) || String.IsNullOrWhiteSpace(p.UserName))
                {
                    return "A user has a empty name or user name.";
                }
                if (p.Coins < 0)
                {
                    return $"User {p.UserName} has less than 0 coins";
                }
                if(p.OwnedBuildings == null)
                {
                    return $"Player {p.UserName} owned buildings object is null";
                }
                if(p.OwnedBuildings.Count != m_baseHelper.BuildingRules.GetCountOfUniqueTypes())
                {
                    return $"Player {p.UserName}'s owned building list is too short";
                }
                if(p.UserName.Equals(m_perspectivePlayerUserName))
                {
                    if(m_perspectivePlayerIndex != count)
                    {
                        return $"The perspective player index in the state helper is incorrect.";
                    }
                    matchedPerspective = true;
                }
                for(int bi = 0; bi < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); bi++)
                {
                    int numberOwned = p.OwnedBuildings[bi];
                    if(numberOwned < 0)
                    {
                        return $"Player {p.UserName}'s building count for building {bi} is < 0";
                    }
                    if (numberOwned > m_baseHelper.Marketplace.GetMaxBuildingsPerPlayer(bi))
                    {
                        return $"Player {p.UserName}'s building count for building {bi} is greater than the max that can be owned per player.";
                    }
                }
                count++;
            }
            if (!matchedPerspective)
            {
                return $"The perspective user name {m_perspectivePlayerUserName} didn't match and player user names";
            }
            return null;
        }


        /// <summary>
        /// Returns the user name perspective of the player all default question will be answered from.
        /// </summary>
        /// <returns></returns>
        public string GetPerspectiveUserName()
        {
            return m_perspectivePlayerUserName;
        }

        /// <summary>
        /// Returns the player index of the perspective player the state helper is looking at.
        /// </summary>
        /// <returns></returns>
        public int GetPerspectivePlayerIndex()
        {
            return m_perspectivePlayerIndex;
        }

        /// <summary>
        /// Sets a new user perspective. If invalid the function will throw.
        /// </summary>
        /// <param name="newUserName">The perspective user name</param>
        public void SetPerspectiveUserName(string playerUserName)
        {
            // Validate the user name
            if(String.IsNullOrWhiteSpace(playerUserName))
            {
                throw GameErrorException.Create(m_baseHelper.GetState(), ErrorTypes.InvalidState, $"The player state helper was passed an invalid user name.", false);
            }

            // Reset the vars.
            m_perspectivePlayerIndex = -1;
            m_perspectivePlayerUserName = null;

            // Find the user's index
            GameState s = m_baseHelper.GetState();
            for (int pi = 0; pi < s.Players.Count; pi++)
            {
                if (s.Players[pi].UserName.Equals(playerUserName))
                {
                    m_perspectivePlayerIndex = pi;
                    break;
                }
            }

            // Validate
            if(m_perspectivePlayerIndex == -1)
            {
                throw GameErrorException.Create(m_baseHelper.GetState(), ErrorTypes.InvalidState, $"The player state helper was passed a user name that wasn't found in the player list.", false);
            }

            // Set the final var.
            m_perspectivePlayerUserName = playerUserName;
        }

        #region General Questions

        /// <summary>
        /// Gets the count of players in the game.
        /// </summary>
        /// <returns></returns>
        public int GetPlayerCount()
        {
            GameState s = m_baseHelper.GetState();
            return s.Players.Count;
        }

        /// <summary>
        /// Validates that a player index is valid for this game.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>true if it's ok; false if not</returns>
        public bool ValidatePlayerIndex(int index)
        {
            GameState s = m_baseHelper.GetState();
            if (index < 0 || index >= s.Players.Count)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates is a player user name is valid for this game.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns>True if it's valid; false otherwise.</returns>
        public bool ValidatePlayer(string userName)
        {
            if(String.IsNullOrWhiteSpace(userName))
            {
                return false;
            }
            GameState s = m_baseHelper.GetState();
            foreach (GamePlayer p in s.Players)
            {
                if (p.UserName.Equals(userName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a player has won the game. 
        /// </summary>
        /// <returns>If a winner is found, the player is returned. Otherwise, null.</returns>
        public GamePlayer CheckForWinner()
        {
            GameState s = m_baseHelper.GetState();
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

        /// <summary>
        /// Returns the current ranking of all the players, considering how many landmarks they own.
        /// </summary>
        /// <returns></returns>
        public List<KokkaKoroLeaderboardElement> GetCurrentLeaderboard()
        {
            // Get all of the players and how many buildings they have.
            List<KokkaKoroLeaderboardElement> players = new List<KokkaKoroLeaderboardElement>();
            foreach(GamePlayer p in m_baseHelper.GetState().Players)
            {
                players.Add(new KokkaKoroLeaderboardElement()
                {
                    Player = p,
                    LandmarksOwned = GetNumberOfLandmarksOwned(p.PlayerIndex)
                });
            }

            // Sort
            players.Sort(delegate (KokkaKoroLeaderboardElement x, KokkaKoroLeaderboardElement y)
            {
                return y.LandmarksOwned - x.LandmarksOwned;
            });

            int rank = 0;
            int lastCardCount = 5;
            foreach(KokkaKoroLeaderboardElement p in players)
            {
                if(p.LandmarksOwned < lastCardCount)
                {
                    rank++;
                    lastCardCount = p.LandmarksOwned;
                }
                p.Rank = rank;
            }
            return players;
        }

        #endregion

        #region Basic Player Specific 

        /// <summary>
        /// Gets a game player given a user name.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public GamePlayer GetPlayer(string userName)
        {
            // Validate the user name
            if(!ValidatePlayer(userName))
            {
                return null;
            }

            // Find the user and return them.
            GameState s = m_baseHelper.GetState();
            foreach(GamePlayer p in s.Players)
            {
                if(p.UserName.Equals(userName))
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a game player. If no argument is given, this returns the perspective player.
        /// </summary>
        /// <param name="playerIndex">If null, returns the perspective player.</param>
        /// <returns></returns>
        public GamePlayer GetPlayer(int? playerIndex = null)
        {
            GameState s = m_baseHelper.GetState();
            if (!playerIndex.HasValue)
            {
                // Return the perspective player.
                // These vars are safe to use as long as the object is valid.
                return s.Players[m_perspectivePlayerIndex];
            }
            else
            {
                if(!ValidatePlayerIndex(playerIndex.Value))
                {
                    return null;
                }
                return s.Players[playerIndex.Value];
            }
        }

        /// <summary>
        /// Returns the user name of a player given an index. If no index is given, the perspective user will be used. 
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public string GetPlayerUserName(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if(p == null)
            {
                return null;
            }
            return p.UserName;
        }

        /// <summary>
        /// Returns the player name of a player given an index. If no index is given, the perspective user will be used. 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetPlayerName(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return null;
            }
            return p.Name;
        }

        #endregion

        #region Player Game Stuff

        public int GetNumberOfLandmarksOwned(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return -1;
            }

            int count = 0;
            GameState s = m_baseHelper.GetState();
            for(int b = 0; b < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                if(m_baseHelper.BuildingRules[b].GetEstablishmentColor() == EstablishmentColor.Landmark)
                {
                    count += p.OwnedBuildings[b] > 0 ? 1 : 0;
                }
            }
            return count;
        }

        /// <summary>
        /// Returns the max amount of rolls for the given player. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public int GetMaxRollsAllowed(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return -1;
            }
            // If the player owns a radio tower, they can reroll the dice once per turn if desired..
            return p.OwnedBuildings[BuildingRules.RadioTower] > 0 ? 2 : 1;
        }

        /// <summary>
        /// Returns the max count of dice that the given player can roll. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public int GetMaxCountOfDiceCanRoll(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if(p == null)
            {
                return -1;
            }
            // If the player owns a train station, they can roll two dice.
            return p.OwnedBuildings[BuildingRules.TrainStation] > 0 ? 2 : 1;
        }

        /// <summary>
        /// Returns if the given player can take another turn after their current turn. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public bool CanHaveExtraTurn(int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return false;
            }

            // Make sure they rolled doubles
            GameState s = m_baseHelper.GetState();
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

        /// <summary>
        /// Indicates if the given player and building qualify for the shopping mall +1 coin bonus. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool ShouldGetShoppingMallBonus(int buildingIndex, int? playerIndex = null)
        {
            // Validate
            if(!m_baseHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return false;
            }
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return false;
            }

            // Check if the player has a shopping mall
            if(p.OwnedBuildings[BuildingRules.ShoppingMall] == 0)
            {
                return false;
            }

            // Check if the building qualifies.
            BuildingBase b = m_baseHelper.BuildingRules[buildingIndex];
            if(b.GetEstablishmentProduction() == EstablishmentProduction.Bread || b.GetEstablishmentProduction() == EstablishmentProduction.Cup)
            {
                return true;
            }
            return false;
        }
               
        /// <summary>
        /// Given a building index and player, returns the count of building built. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public int GetBuiltCount(int buildingIndex, int? playerIndex = null)
        {
            // Validate the building index
            if (!m_baseHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return -1;
            }

            // Get the player
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return -1;
            }

            // Get the building they own.
            return p.OwnedBuildings[buildingIndex];
        }

        /// <summary>
        /// Given a building index and player index, returns true or false if the player has hit the limit of building they can build.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool HasReachedBuildingBuiltLimit(int buildingIndex, int? playerIndex = null)
        {
            // This will validate user name and building index.
            int built = GetBuiltCount(buildingIndex, playerIndex);
            if (built == -1)
            {
                return true;
            }
            if (built >= m_baseHelper.Marketplace.GetMaxBuildingsPerPlayer(buildingIndex))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Given a building index and player index, return if the player can afford to buy a building.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CanAffordBuilding(int buildingIndex, int? playerIndex = null)
        {
            // Validate the building index
            if (!m_baseHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                return false;
            }

            GamePlayer p = GetPlayer(playerIndex);
            if(p == null)
            {
                return false;
            }

            // Check if they have the coins to build it.
            if (p.Coins < m_baseHelper.BuildingRules[buildingIndex].GetBuildCost())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Given a building index and player index, returns if the player can building the given building. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="buildingIndex"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool CanBuildBuilding(int buildingIndex, int? playerIndex = null)
        {
            // Check if we can afford it.
            // (this will validate the index and user name)
            if(!CanAffordBuilding(buildingIndex, playerIndex))
            {
                return false;
            }         

            // Check if it's available in the marketplace.
            if(!m_baseHelper.Marketplace.IsBuildableFromMarketplace(buildingIndex))
            {
                return false;
            }

            // Last, check if the player already has the per player building limit.
            if(HasReachedBuildingBuiltLimit(buildingIndex, playerIndex))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Given a desired max and player index, returns the max amount of coins that can be take from this player. If no index is given, the perspective user will be used.
        /// </summary>
        /// <param name="desiredAmount"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public int GetMaxTakeableCoins(int desiredAmount, int? playerIndex = null)
        {
            // Get the player
            GamePlayer p = GetPlayer(playerIndex);
            if (p == null)
            {
                return -1;
            }

            // Return the desired amount or however many coins they have.
            return Math.Min(desiredAmount, p.Coins);
        }

        /// <summary>
        /// Given player index, returns if there are that are affordable and available to build from the marketplace.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public bool AreAvailableBuildingsThatWeCanAfford(int? playerIndex = null)
        {
            // Get all building that are in the marketplace.
            List<int> buildable = m_baseHelper.Marketplace.GetBuildingTypesBuildableInMarketplace();

            // See if there are any we can afford.
            return FilterBuildingIndexesWeCanAfford(buildable, playerIndex).Count > 0;
        }

        /// <summary>
        /// Given player index, returns a list of building indexes that are affordable and available to build from the marketplace.
        /// </summary>
        /// <param name="buildingIndexes"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public List<int> FilterBuildingIndexesWeCanAfford(List<int> buildingIndexes, int? playerIndex = null)
        {
            List<int> canAffordAndBuildable = new List<int>();
            GamePlayer p = GetPlayer(playerIndex);
            if(p == null)
            {
                return canAffordAndBuildable;
            }

            foreach(int b in buildingIndexes)
            {
                // Filter out only the ones we can afford and haven't bought too many of.
                if(CanAffordBuilding(b, playerIndex) && !HasReachedBuildingBuiltLimit(b, playerIndex))
                {
                    canAffordAndBuildable.Add(b);
                }
            }
            return canAffordAndBuildable;
        }  

        /// <summary>
        /// Given a production type and player index, returns the count of buildings the player has that build that matches the production type.
        /// </summary>
        /// <param name="production"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public int GetTotalProductionTypeBuilt(EstablishmentProduction production, int? playerIndex = null)
        {
            GamePlayer p = GetPlayer(playerIndex);
            if(p == null)
            {
                return -1;
            }

            // Count the building count for the given production.
            int total = 0;
            for (int buildingIndex = 0; buildingIndex < m_baseHelper.BuildingRules.GetCountOfUniqueTypes(); buildingIndex++)
            {
                BuildingBase b = m_baseHelper.BuildingRules[buildingIndex];
                if(b.GetEstablishmentProduction() == production)
                {
                    total += GetBuiltCount(buildingIndex, playerIndex);
                }
            }
            return total;
        }

        #endregion
    }
}

