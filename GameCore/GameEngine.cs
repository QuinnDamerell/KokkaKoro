using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameCommon;
using GameCommon.Protocol;
using GameCommon.Protocol.ActionOptions;
using GameCommon.Protocol.GameUpdateDetails;
using GameCommon.StateHelpers;
using Newtonsoft.Json.Linq;

namespace GameCore
{
    public class InitalPlayer
    {
        public string UserName;
        public string FriendlyName;
    }

    public class GameEngine : IRandomGenerator
    {
        GameState m_state;
        bool m_gameStarted = false;
        LogKeeper m_logKeeper;
        RNGCryptoServiceProvider m_random = new RNGCryptoServiceProvider();

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

            // Check if the game has not been started yet. 
            // This setup needs to happen first, because the validation will fail.
            if (!m_gameStarted)
            {
                StartGame(actionLog, stateHelper);
                return;
            }       

            // Check the state.
            ThrowIfInvalidState(stateHelper);       

            // Add the action to the game log. (even if this fails we want to record it)
            actionLog.Add(GameLog.CreateAction(m_state, action));

            // Make sure it's this user's turn.
            ValidateUserTurn(action, stateHelper);

            // Validate the user can currently take the action they are trying to.
            if (!stateHelper.CurrentTurn.CanTakeAction(action.Action))
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"In the current turn state, it's not valid to {action.Action}.", true);
            }

            // Handle the action.
            switch (action.Action)
            {
                case GameActionType.RollDice:
                    HandleRollDiceAction(actionLog, action, stateHelper);
                    break;
                case GameActionType.CommitDiceResult:
                    HandleDiceRollCommitAction(actionLog, action, stateHelper);
                    break;
                case GameActionType.BuyBuilding:

                default:
                    throw GameError.Create(m_state, ErrorTypes.UknownAction, $"Unknown action type. {action.Action}", true);
            }

            // Check if the turn is over.

            // Once the actions have been made, generate the new set of options for the player.
            BuildPlayerActionRequest(actionLog, stateHelper);

            // TODO
        }

        private void SetupGame(List<InitalPlayer> players, GameMode mode)
        {
            // Create our state object.
            m_state = new GameState();
            m_state.Mode = mode;   

            // Setup current state.
            m_state.CurrentTurnState = new TurnState();

            // Create a temp building list we will use to setup the marketplace
            BuildingList buildingList = new BuildingList(mode);

            // Setup the marketplace
            m_state.Market = Marketplace.Create(buildingList);

            // Add the players.
            foreach (InitalPlayer p in players)
            {
                // Create a player.
                GamePlayer gamePlayer = new GamePlayer() { Name = p.FriendlyName, UserName = p.UserName, Coins = 3 };

                // Allocate a space for every building they can own. For starting buildings, give them one.
                for (int i = 0; i < buildingList.GetCount(); i++)
                {
                    if (buildingList[i].IsStartingBuilding())
                    {
                        gamePlayer.OwnedBuildings.Add(1);
                    }
                    else
                    {
                        gamePlayer.OwnedBuildings.Add(0);
                    }
                }

                // Add the player.
                m_state.Players.Add(gamePlayer);
            }

            // Adding building to the marketplace
            m_state.Market.ReplenishMarket(this, m_state.GetStateHelper(String.Empty));
        }

        private void StartGame(List<GameLog> log, StateHelper stateHelper)
        {
            m_gameStarted = true;

            // Broadcast a game start event.
            log.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.GameStart, "Game Starting!", null));

            // And add a request for the first player to go.
            BuildPlayerActionRequest(log, stateHelper);
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
                if (action.Options is JObject obj)
                {
                    options = obj.ToObject<RollDiceOptions>();
                }
            }
            catch (Exception e)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Failed to parse roll dice options: {e.Message}", true);
            }
            if (options.DiceCount < 0)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Number of dice to roll must be > 0", true);
            }
            if (options.DiceCount > stateHelper.Player.GetMaxDiceCountCanRoll())
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"This player can't roll that many dice; they have a max of {stateHelper.Player.GetMaxDiceCountCanRoll()} currently.", true);
            }

            // Roll the dice!
            m_state.CurrentTurnState.Rolls++;
            m_state.CurrentTurnState.DiceResults.Clear();
            int sum = 0;
            for (int i = 0; i < options.DiceCount; i++)
            {
                int result = RandomInt(1, 6);
                sum += result;
                m_state.CurrentTurnState.DiceResults.Add(result);
            }

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            // Create an update
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.DiceRollResult, $"Player {stateHelper.GetPerspectiveUserName()} rolled {sum}.",
                new DiceRollDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, RolledForPlayerIndex = stateHelper.Player.GetPlayerIndex() }));

            if (options.AutoCommitResult)
            {
                HandleDiceRollCommitAction(log, GameAction<object>.CreateCommitDiceResult(), stateHelper);
            }
        }

        private void HandleDiceRollCommitAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // This doesn't have options, so there's no need to validate them.

            //// Commit the dice roll.
            //m_state.CurrentTurnState.Rolls++;
            //m_state.CurrentTurnState.DiceResults.Clear();
            //int sum = 0;
            //for (int i = 0; i < options.DiceCount; i++)
            //{
            //    int result = RandomInteger(1, 6);
            //    sum += result;
            //    m_state.CurrentTurnState.DiceResults.Add(result);
            //}

            //// Validate things are good.
            //ThrowIfInvalidState(stateHelper);

            //// Create an update
            //log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.DiceRollResult, $"Player {stateHelper.GetPerspectiveUserName()} rolled {sum}.",
            //    new DiceRollDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, RolledForPlayerIndex = stateHelper.Player.GetPlayerIndex() }));

            //if (options.AutoCommitResult)
            //{
            //    HandleDiceRollCommitAction(log, GameAction<object>.CreateCommitDiceResult(), stateHelper);
            //}
        }

        private void ThrowIfInvalidState(StateHelper stateHelper)
        {
            string err = stateHelper.Validate();
            if(!String.IsNullOrWhiteSpace(err))
            {
                // todo this should end the game.
                throw GameError.Create(m_state, ErrorTypes.InvalidState, $"The game is now in an invalid state. Error: {err}", false);
            }
        }

        // Returns a random int.
        public int RandomInt(int minInclusive, int maxInclusive)
        {
            int maxExclusive = maxInclusive + 1;
            long diff = (long)maxExclusive - minInclusive;
            long upperBound = uint.MaxValue / diff * diff;

            uint randomUInt;
            do
            {
                byte[] buffer = new byte[4];
                m_random.GetBytes(buffer);
                randomUInt = BitConverter.ToUInt32(buffer, 0);     
            } while (randomUInt >= upperBound);

            return (int)(minInclusive + (randomUInt % diff));
        }
    }
}
