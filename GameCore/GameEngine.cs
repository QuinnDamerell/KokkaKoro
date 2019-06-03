using System;
using System.Collections.Generic;
using GameCommon;
using GameCommon.Protocol;

namespace GameCore
{
    public class InitalPlayer
    {
        public string UserName;
        public string FriendlyName;
    }

    public class GameEngine
    {
        GameState m_state;
        bool m_gameStarted = false;
        LogKeeper m_logKeeper;

        public GameEngine(List<InitalPlayer> players, GameMode mode)
        {
            // Create a new logger.
            m_logKeeper = new LogKeeper();

            // Build the game initial state.
            SetupGame(players, mode);
        }

        public List<GameLog> ConsumeAction()
        {
            // Handle the action.
            List<GameLog> actionLog = ConsumeActionInternal();

            // Make sure we add all of the events to the game log.
            // TOOD add incoming action.
            m_logKeeper.AddToLog(actionLog);

            return actionLog;
        }

        private List<GameLog> ConsumeActionInternal()
        {
            List<GameLog> actionLog = new List<GameLog>();

            // Check if this is the first action on the game.
            if (!m_gameStarted)
            {
                StartGame(actionLog);
                return actionLog;
            }

            return actionLog;
        }

        private void StartGame(List<GameLog> log)
        {
            m_gameStarted = true;

            // Broadcast a game start event.
            log.Add(GameLog.CreateGameUpdate(m_state, "Game Start!"));

            // And add a request for the first player to go.
            BuildPlayerActionRequest(log);
        }

        private void SetupGame(List<InitalPlayer> players, GameMode mode)
        {
            m_state = new GameState();

            // Add the players
            foreach(InitalPlayer p in players)
            {
                m_state.Players.Add(new GamePlayer() { Name = p.FriendlyName, UserName = p.UserName, Coins = 3 });
            }

            // Give each player their starting building


            // Adding building to the marketplace     

            // Init the rest of the state
            m_state.CurrentPlayerIndex = 0;
            m_state.TurnState = TurnState.WaitingOnRoll;
        }

        private void BuildPlayerActionRequest(List<GameLog> log)
        {
            // Based on the current state, build a list of possible actions.
            List<GameActionType> actions = new List<GameActionType>();
            switch(m_state.TurnState)
            {
                case TurnState.WaitingOnRoll:
                    actions.Add(GameActionType.RollDice);
                    break;
            }

            // Build a action request object
            log.Add(GameLog.CreateActionRequest(m_state, actions));
        }
    }
}
