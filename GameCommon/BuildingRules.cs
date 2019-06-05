using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    // This class is used to list and get references to the building types. Since we don't send the building 
    // details in each game state, this is where you can find each building given it's index and query it for
    // the name and rules of the building.
    public class BuildingRules
    {
        List<BuildingBase> m_bulidings = new List<BuildingBase>();

        public BuildingRules(GameMode mode)
        {
            // For all modes, include the base buildings.
            m_bulidings.Clear();
            m_bulidings.Add(new WheatField());
            m_bulidings.Add(new Bakery());
        }

        public BuildingBase Get(int buildingIndex)
        {
            return m_bulidings[buildingIndex];
        }

        public BuildingBase this[int i]
        {
            get { return Get(i); }
        }

        public int GetCountOfUniqueTypes()
        {
            return m_bulidings.Count;
        }
    }
}
