using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.Interfaces
{
    interface IPlayerHostControl
    {
        void OnRegister();

        void OnGameUpdate();
        void OnTurn();
    }
}
