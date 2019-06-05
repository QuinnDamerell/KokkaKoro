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

        // Returns the number of unique building types are still available in the game.
        public int GetCountOfAvailableUniqueBuidingTypesInGame()
        {
            GameState s = m_gameHelper.GetState();
            for (int b = 0; b < m_gameHelper.Buildings.GetCount(); b++)
            {

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
    }
}
