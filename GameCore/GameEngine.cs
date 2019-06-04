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

        public (GameActionResponse, List<GameLog>) ConsumeAction(GameAction<object> action, string userName)
        {
            // Handle the action, 
            List<GameLog> actionLog = new List<GameLog>();
            try
            {
                ConsumeActionInternal(action, userName, actionLog);
            }
            catch(GameError e)
            {
                // If the call throws an action exception, there was something wrong with the action. 

                // Add the error to the log.
                actionLog.Add(GameLog.CreateError(e));

                // Return the error and the action log.                
                return (GameActionResponse.CreateError(e), actionLog);
            }
            catch(Exception e)
            {
                // If this exception was thrown, it's most likely a bug.  

                // Create an error and add it to the log.
                GameError err = GameError.Create(m_state, ErrorTypes.Unknown, $"An exception was thrown while handling action. {e.Message}", false);
                actionLog.Add(GameLog.CreateError(err));
                
                // Return the error.
                return (GameActionResponse.CreateError(err), actionLog);
            }
            finally
            {
                // Make sure we add all of the events to the game log.
                m_logKeeper.AddToLog(actionLog);
            }   
            
            // On success, send  the success response and the log.
            return (GameActionResponse.CreateSuccess(), actionLog);
        }

        private void ConsumeActionInternal(GameAction<object> action, string userName, List<GameLog> actionLog)
        {
            // A few notes. 
            // Since this action was invoked by a player action, if the action fails it will be sent directly back to the user.
            // If there are any errors handling the action, they should throw the GameError exception which will be added to the
            // game log automatically.

            // Check if this is the first action on the game.
            if (!m_gameStarted)
            {
                StartGame(actionLog);
                return;
            }

            // Add the action to the game log. (even if this fails we want to record it)
            actionLog.Add(GameLog.CreateAction(m_state, action));

            // Make sure it's this user's turn.
            ValidateUserTurn(action, userName);

            switch(action.Action)
            {
                case GameActionType.RollDice:
                    break;
                default:
                    throw GameError.Create(m_state, ErrorTypes.UknownAction, $"Unknown action type. {action.Action}", true);
            }     



            // TODO
        }

        private void StartGame(List<GameLog> log)
        {
            m_gameStarted = true;

            // Broadcast a game start event.
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.GameStart, "Game Starting!"));

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

        private void ValidateUserTurn(GameAction<object> action, string userName)
        {
            // Next make sure we have a user and it's their turn.
            bool foundUser = false;
            for (int i = 0; i < m_state.Players.Count; i++)
            {
                if (m_state.Players[i].UserName.Equals(userName))
                {
                    foundUser = true;
                    if (i != m_state.CurrentPlayerIndex)
                    {
                        throw GameError.Create(m_state, ErrorTypes.NotPlayersTurn, $"`{userName}` tried to send a action when it's not their turn.", false);
                    }
                    break;
                }
            }
            if (!foundUser)
            {
                throw GameError.Create(m_state, ErrorTypes.PlayerUserNameNotFound, $"`{userName}` user name wasn't found in this game.", false);
            }

            // Next, make sure we have an action.
            if (action == null)
            {
                throw GameError.Create(m_state, ErrorTypes.Unknown, $"No action object was sent", false);
            }
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
