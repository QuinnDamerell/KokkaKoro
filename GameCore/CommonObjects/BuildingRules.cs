using GameCore.CommonObjects.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects
{
    // This class is used to list and get references to the building types. Since we don't send the building 
    // details in each game state, this is where you can find each building given it's index and query it for
    // the name and rules of the building.
    public class BuildingRules
    {
        static List<BuildingBase> m_bulidings = new List<BuildingBase>();

        public void Configure(GameMode mode)
        {
            // For all modes, include the base buildings.
            m_bulidings.Clear();
            m_bulidings.Add(new WheatField());
            m_bulidings.Add(new Bakery());


            // todo
            // Add expansion buildings
        }        


        public static BuildingBase Get(int buildingIndex)
        {
            return m_bulidings[buildingIndex];
        }

        public static int GetBuildingCount()
        {
            return m_bulidings.Count;
        }
    }
}
