using System;
using System.Collections.Generic;
using GameCommon;
using GameCommon.Protocol;
using GameCommon.Protocol.ActionOptions;
using GameCommon.StateHelpers;
using Newtonsoft.Json.Linq;

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

            // Build a state helper.
            StateHelper stateHelper = m_state.GetStateHelper(userName);

            // Check if this is the first action on the game.
            if (!m_gameStarted)
            {
                StartGame(actionLog, stateHelper);
                return;
            }

            // Add the action to the game log. (even if this fails we want to record it)
            actionLog.Add(GameLog.CreateAction(m_state, action));

            // Make sure it's this user's turn.
            ValidateUserTurn(action, stateHelper);

            switch(action.Action)
            {
                case GameActionType.RollDice:
                    HandleRollDiceAction(actionLog, action, stateHelper);
                    break;
                default:
                    throw GameError.Create(m_state, ErrorTypes.UknownAction, $"Unknown action type. {action.Action}", true);
            }     



            // TODO
        }

        private void StartGame(List<GameLog> log, StateHelper stateHelper)
        {
            m_gameStarted = true;

            // Broadcast a game start event.
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.GameStart, "Game Starting!"));

            // And add a request for the first player to go.
            BuildPlayerActionRequest(log, stateHelper);
        }

        private void SetupGame(List<InitalPlayer> players, GameMode mode)
        {
            m_state = new GameState();

            // Add the players
            foreach(InitalPlayer p in players)
            {
                m_state.Players.Add(new GamePlayer() { Name = p.FriendlyName, UserName = p.UserName, Coins = 3 });
            }

            // Setup the current player object
            m_state.CurrentTurnState = new TurnState();
            m_state.CurrentTurnState.PlayerIndex = 0;
            m_state.CurrentTurnState.Clear(0);
            
            // Give each player their starting building


            // Adding building to the marketplace     
        }

        private void ValidateUserTurn(GameAction<object> action, StateHelper stateHelper)
        {
            // Next make sure we have a user and it's their turn.
            int playerIndex = stateHelper.Player.GetPlayerIndex();
            if(playerIndex == -1)
            {
                throw GameError.Create(m_state, ErrorTypes.PlayerUserNameNotFound, $"`{stateHelper.GetPerspectiveUserName()}` user name wasn't found in this game.", false);
            }
            if(!stateHelper.CurrentTurn.IsMyTurn())
            {
                throw GameError.Create(m_state, ErrorTypes.NotPlayersTurn, $"`{stateHelper.GetPerspectiveUserName()}` tried to send a action when it's not their turn.", false);
            }

            // Next, make sure we have an action.
            if (action == null)
            {
                throw GameError.Create(m_state, ErrorTypes.Unknown, $"No action object was sent", false);
            }
        }

        private void BuildPlayerActionRequest(List<GameLog> log, StateHelper stateHelper)
        {
            // Based on the current state, build a list of possible actions.
            List<GameActionType> actions = stateHelper.CurrentTurn.GetPossibleActions();

            // Build a action request object
            log.Add(GameLog.CreateActionRequest(m_state, actions));
        }

        private void HandleRollDiceAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // Try to get the options
            RollDiceOptions options = null;
            try
            {
                if(action.Options is JObject obj)
                {
                    options = obj.ToObject<RollDiceOptions>();
                }
            }
            catch(Exception e)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Failed to parse roll dice options: {e.Message}", true);
            }
            if(options.DiceCount < 0)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Number of dice to roll must be > 0", true);
            }
            if(options.DiceCount > stateHelper.Player.MaxDiceCountCanRoll())
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"This player can't has a max of {stateHelper.Player.MaxDiceCountCanRoll()} they can roll currently.", true);
            }

            // todo

        }
    }
}
