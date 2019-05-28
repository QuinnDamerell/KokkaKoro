using GameCore.CommonObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCore.Interfaces
{
    interface IPlayerHost
    {
        void OnGameBegin(IPlayerControl controller, GameState initalState);

        void OnGameUpdate(GameState initalState);

        void OnTurn(GameState gameState);
    }
}
