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
        // If you want or need to access a building by index directly, you should use these 
        // values to make sure things stay in sync.
        public readonly static int WheatField = 0;
        public readonly static int Ranch = 1;
        public readonly static int Bakery = 2;
        public readonly static int ConvenienceStore = 3;
        public readonly static int Forest = 4;
        public readonly static int CheeseFactory = 5;
        public readonly static int FurnitureFactory = 6;
        public readonly static int Mine = 7;
        public readonly static int AppleOrchard = 8;
        public readonly static int FarmersMarket = 9;
        public readonly static int TrainStation = 10;
        public readonly static int ShoppingMall = 11;
        public readonly static int RadioTower = 12;
        public readonly static int AmusementPark = 13;

        List<BuildingBase> m_bulidings = new List<BuildingBase>();

        public BuildingRules(GameMode mode)
        {
            // For all modes, include the base buildings.
            m_bulidings.Clear();
            m_bulidings.Add(new WheatField(0));
            m_bulidings.Add(new Ranch(1));
            m_bulidings.Add(new Bakery(2));
            m_bulidings.Add(new ConvenienceStore(3));
            m_bulidings.Add(new Forest(4));
            m_bulidings.Add(new CheeseFactory(5));
            m_bulidings.Add(new FurnitureFactory(6));
            m_bulidings.Add(new Mine(7));
            m_bulidings.Add(new AppleOrchard(8));
            m_bulidings.Add(new FarmersMarket(9));
            m_bulidings.Add(new TrainStation(10));
            m_bulidings.Add(new ShoppingMall(11));
            m_bulidings.Add(new RadioTower(12));
            m_bulidings.Add(new AmusementPark(13));
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
