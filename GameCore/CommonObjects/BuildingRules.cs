using GameCore.CommonObjects.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.CommonObjects
{
    public class BuildingRules
    {
        static List<BuildingBase> m_bulidings = new List<BuildingBase>();
        
        public static void Setup(GameMode mode)
        {
            if(mode == GameMode.Base)
            {
                m_bulidings.Add(new WheatField());
                m_bulidings.Add(new Bakery());
            }
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
