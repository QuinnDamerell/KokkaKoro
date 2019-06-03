using GameCore.CommonObjects;
using System;
using System.Collections.Generic;

namespace GameCore
{
    public enum GameMode
    {
        BaseGame,
        // todo expansions
    }

    public class GameEngine
    {
        GameState m_state;

        public GameEngine(List<string> players, GameMode mode)
        {
            // Build the game initial state.
            SetupGame(players, mode);
        }

        public void TakeOneTurn()
        {

        }

        private void SetupGame(List<string> players, GameMode mode)
        {
            m_state = new GameState();

            // Add the players
            foreach(string p in players)
            {
                m_state.Players.Add(new GamePlayer() { Name = p, Coins = 3 });
            }

            // Give each player their starting building


            // Adding building to the marketplace

            
        }
    }
}
