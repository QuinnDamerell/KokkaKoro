﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameCommon;
using GameCommon.BuildingActivations;
using GameCommon.Buildings;
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

    public class GameEngine
    {
        // The engine state.
        GameState m_state;

        // Keeps track of the game log.
        readonly LogKeeper m_logKeeper;

        // Gives us random numbers
        readonly RandomGenerator m_random = new RandomGenerator();

        // Used to make sure only one action is handled at a time.
        readonly object m_actionLock = new object();

        // Limits the game in terms of rounds.
        readonly int? m_roundLimit = null;

        public GameEngine(List<InitalPlayer> players, GameMode mode, int? roundLimit = null)
        {
            // Create a new logger.
            m_logKeeper = new LogKeeper();

            // Set the round limit.
            m_roundLimit = roundLimit;

            // Build the game initial state.
            SetupGame(players, mode);
        }

        // Returns the current game logs.
        public List<GameLog> GetLogs()
        {
            return m_logKeeper.GetLogs();
        }

        // Returns the current state object of the game.
        public GameState GetState()
        {
            return m_state;
        }

        // Indicates if th game has ended or not yet.
        public bool HasEnded()
        {
            return m_state.CurrentTurnState.HasGameEnded;
        }

        public (bool, List<KokkaKoroLeaderboardElement>) GetResults()
        {
            if(m_state == null || m_state.Players == null || m_state.Players.Count == 0)
            {
                return (false, null);
            }
            lock (m_actionLock)
            {
                StateHelper sh = m_state.GetStateHelper(m_state.Players[0].UserName);
                return (sh.Player.CheckForWinner() != null, sh.Player.GetCurrentLeaderboard());
            }
        }

        // Take a play action and handles it.
        public (GameActionResponse, List<GameLog>) ConsumeAction(GameAction<object> action, string userName)
        {
            // We only want to allow one action to be attempted at a time.
            lock (m_actionLock)
            {
                List<GameLog> actionLog = new List<GameLog>();

                // Handle the action.
                try
                {
                    ConsumeActionInternal(action, userName, actionLog);
                }
                catch (GameErrorException e)
                {
                    // If the call throws an action exception, there was something wrong with the action. 

                    // Add the error to the log.
                    actionLog.Add(GameLog.CreateError(e.GetGameError()));

                    // If this was a fatal error, end the game.
                    if (e.IsFatal)
                    {
                        EndGameInternal(actionLog, GameEndReason.GameEngineError);
                    }   

                    // Return the error and the action log.                
                    return (GameActionResponse.CreateError(e.GetGameError()), actionLog);
                }
                catch (Exception e)
                {
                    // If this exception was thrown, it's most likely a bug.  

                    // Create an error and add it to the log.
                    GameError err = GameError.Create(m_state, ErrorTypes.Unknown, $"An exception was thrown while handling action. {e.Message}", false);
                    actionLog.Add(GameLog.CreateError(err));

                    // This is a fatal error, end the game.         
                    EndGameInternal(actionLog, GameEndReason.GameEngineError);

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
        }

        // Ends the game, given a reason.
        public (GameActionResponse, List<GameLog>) EndGame(GameEndReason reason)
        {
            // Create a action log.
            List<GameLog> actionLog = new List<GameLog>();

            // End the game.
            try
            {
                EndGameInternal(actionLog, reason);
            }
            catch (GameErrorException e)
            {
                // Add the error to the log.
                actionLog.Add(GameLog.CreateError(e.GetGameError()));
            }
            catch (Exception e)
            {
                // Create an error and add it to the log.
                GameError err = GameError.Create(m_state, ErrorTypes.Unknown, $"An exception was thrown while ending the game. {e.Message}", false);
                actionLog.Add(GameLog.CreateError(err));
            }

            // Make sure we add all of the events to the game log.
            m_logKeeper.AddToLog(actionLog);

            // Send back the results.
            return (GameActionResponse.CreateSuccess(), actionLog);
        }

        private GameActionResponse EndGameInternal(List<GameLog> actionLog, GameEndReason reason, StateHelper stateHelper = null)
        {
            // Note we don't take the action lock here, so if some game is stuck processing it will still get killed.
            if(m_state.CurrentTurnState.HasGameEnded)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.GameEnded, $"The game has already been ended.", false, true);
            }
            
            // End the game by setting the game ended flag.
            m_state.CurrentTurnState.HasGameEnded = true;

            // Always try to find a winner no matter the reason.
            GamePlayer winner = null;
            try
            {
                // If we didn't get a state helper just make one. 
                if (stateHelper == null)
                {
                    stateHelper = m_state.GetStateHelper(m_state.Players[m_state.CurrentTurnState.PlayerIndex].UserName);
                }

                // Create an empty state helper to check for a winner.
                winner = stateHelper.Player.CheckForWinner();
            }
            catch (GameErrorException e)
            {
                // Add the error to the log.
                actionLog.Add(GameLog.CreateError(e.GetGameError()));
            }
            catch (Exception e)
            {
                // Create an error and add it to the log.
                GameError err = GameError.Create(m_state, ErrorTypes.Unknown, $"An exception was thrown while ending the game. {e.Message}", false);
                actionLog.Add(GameLog.CreateError(err));
            }

            // If we have a reason, ignore the current reason and make it a winner.
            int? playerIndex = null;
            if (winner != null)
            {
                reason = GameEndReason.Winner;
                playerIndex = winner.PlayerIndex;
            }

            // Log!
            actionLog.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.GameEnd, 
                $"{(winner != null ? $"{winner.Name} has won! " : "")}The game has ended because '{reason.ToString()}'", true,
                new GameEndDetails() { PlayerIndex = playerIndex, Reason = reason }));

            // Return success.
            return GameActionResponse.CreateSuccess();            
        }

        private void ConsumeActionInternal(GameAction<object> action, string userName, List<GameLog> actionLog)
        {
            // A few notes. 
            // Since this action was invoked by a player action, if the action fails it will be sent directly back to the user.
            // If there are any errors handling the action, they should throw the GameError exception which will be added to the
            // game log automatically.

            // Build a state helper for the current user.
            StateHelper stateHelper = m_state.GetStateHelper(userName);

            // Check if the game has not been started yet. 
            // This setup needs to happen first, because the validation will fail.
            if (!stateHelper.CurrentTurn.HasGameStarted())
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
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"In the current turn state, it's not valid to {action.Action}.", true, false);
            }

            // If there are special actions, handle them.
            if (stateHelper.CurrentTurn.HasPendingSpecialActions())
            {
                HandleSpecialAction(actionLog, action, stateHelper);
            }
            else
            {
                // Handle the action.
                switch (action.Action)
                {
                    case GameActionType.RollDice:
                        HandleRollDiceAction(actionLog, action, stateHelper);
                        break;
                    case GameActionType.CommitDiceResult:
                        HandleDiceRollCommitAction(actionLog, action, stateHelper);
                        break;
                    case GameActionType.BuildBuilding:
                        HandleBuildAction(actionLog, action, stateHelper);
                        break;
                    case GameActionType.EndTurn:
                        HandleEndTurnAction(actionLog, action, stateHelper);
                        break;
                    case GameActionType.Forfeit:
                        EndGameInternal(actionLog, GameEndReason.PlayerForfeit, stateHelper);
                        // Return after we end the game.
                        return;
                    default:
                        throw GameErrorException.Create(m_state, ErrorTypes.UknownAction, $"Unknown action type. {action.Action}", true, false);
                }
            }

            // Check to see if the action committed made the player win.
            GamePlayer player = stateHelper.Player.CheckForWinner();
            if (player != null)
            {
                // We have a winner!
                EndGameInternal(actionLog, GameEndReason.Winner, stateHelper);
                return;
            }

            // Check if the turn is over.
            if (stateHelper.CurrentTurn.HasEndedTurn())
            {
                AdvanceToNextPlayer(actionLog, stateHelper);
            }

            // After advancing, check if we hit the round limit.
            if(m_roundLimit.HasValue &&  m_state.CurrentTurnState.RoundNumber >= m_roundLimit.Value)
            {
                EndGameInternal(actionLog, GameEndReason.RoundLimitReached, stateHelper);
                return;
            }

            // Once the actions have been made, generate the new set of options for the player.
            BuildPlayerActionRequest(actionLog, stateHelper);
        }

        private void SetupGame(List<InitalPlayer> players, GameMode mode)
        {
            // Create our state object.
            m_state = new GameState();
            m_state.Mode = mode;   

            // Setup current state.
            m_state.CurrentTurnState = new TurnState();

            // Create a temp building list we will use to setup the marketplace
            BuildingRules buildingRules = new BuildingRules(mode);

            // Setup the marketplace
            m_state.Market = Marketplace.Create(buildingRules);

            // Add the players.
            int count = 0;
            foreach (InitalPlayer p in players)
            {
                // Create a player.
                GamePlayer gamePlayer = new GamePlayer() { Name = p.FriendlyName, UserName = p.UserName, Coins = 3, PlayerIndex = count };

                // Allocate a space for every building they can own. For starting buildings, give them one.
                for (int i = 0; i < buildingRules.GetCountOfUniqueTypes(); i++)
                {
                    if (buildingRules[i].IsStartingBuilding())
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
                count++;
            }

            // Adding building to the marketplace
            string userName = m_state.Players[m_state.CurrentTurnState.PlayerIndex].UserName;
            m_state.Market.ReplenishMarket(m_random, m_state.GetStateHelper(userName));
        }

        private void StartGame(List<GameLog> log, StateHelper stateHelper)
        {
            if(m_state.CurrentTurnState.HasGameEnded)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, "The game has ended, it can't be started.", false, false);
            }

            // Set the flag.
            m_state.CurrentTurnState.HasGameStarted = true;

            // Broadcast a game start event.
            log.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.GameStart, "Game Starting!", true, null));

            // And add a request for the first player to go.
            BuildPlayerActionRequest(log, stateHelper);
        }

        private void ValidateUserTurn(GameAction<object> action, StateHelper stateHelper)
        {
            // Next make sure we have a user and it's their turn.
            GamePlayer player = stateHelper.Player.GetPlayer();
            if(player.PlayerIndex == -1)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.PlayerUserNameNotFound, $"`{stateHelper.Player.GetPlayerUserName()}` user name wasn't found in this game.", false, false);
            }
            if(!stateHelper.CurrentTurn.IsMyTurn())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.NotPlayersTurn, $"`{stateHelper.Player.GetPlayerUserName()}` tried to send a action when it's not their turn.", false, false);
            }

            // Next, make sure we have an action.
            if (action == null)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.Unknown, $"No action object was sent", false, false);
            }

            // Last, make sure the game hasn't ended.
            if(stateHelper.CurrentTurn.HasGameEnded())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.GameEnded, $"`{stateHelper.Player.GetPlayerUserName()}` can't take an action because the game has ended.", false, false);
            }
        }

        private void AdvanceToNextPlayer(List<GameLog> actionLog, StateHelper stateHelper)
        {
            if(m_state.CurrentTurnState.Rolls == 0 || m_state.CurrentTurnState.DiceResults.Count == 0 || !stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"The turn can't be advanced because the current turn hasn't rolled yet.", true, false);
            }
            if (!stateHelper.CurrentTurn.HasEndedTurn())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"The current turn isn't over, so we can't go to the next player.", false, false);
            }

            // Find the next player.
            int newPlayerIndex = m_state.CurrentTurnState.PlayerIndex;
            newPlayerIndex++;

            // Check if the player has the amusement park and rolled doubles, they get to take another turn.
            if (stateHelper.Player.CanHaveExtraTurn())
            {
                // If so, set them to be the player again.
                newPlayerIndex = m_state.CurrentTurnState.PlayerIndex;

                // Log it
                actionLog.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.ExtraTurn, $"{stateHelper.Player.GetPlayer().Name} rolled double {m_state.CurrentTurnState.DiceResults[0]}s and has an Amusement Park, so they get to take an extra turn!", newPlayerIndex, null));
            }

            // Check for a player index roll over.
            if (newPlayerIndex >= m_state.Players.Count)
            {
                // If the players have rolled over, this is a new round.
                newPlayerIndex = 0;
                m_state.CurrentTurnState.RoundNumber++;
            }

            // And finally reset the current turn and set the new player.
            m_state.CurrentTurnState.Clear(newPlayerIndex);

            // It's also super important to update the state helper with the new user perspective.
            stateHelper.Player.SetPerspectiveUserName(stateHelper.Player.GetPlayerUserName(newPlayerIndex));
        }

        private void BuildPlayerActionRequest(List<GameLog> log, StateHelper stateHelper)
        {
            // Based on the current state, build a list of possible actions.
            List<GameActionType> actions = stateHelper.CurrentTurn.GetPossibleActions();

            if (actions.Count == 0)
            {
                stateHelper.CurrentTurn.GetPossibleActions();
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"There are no possible actions for the current player.", false, true);
            }

            // Build a action request object
            log.Add(GameLog.CreateActionRequest(m_state, actions));
        }

        #region Action Handlers

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
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidActionOptions, $"Failed to parse roll dice options: {e.Message}", true, false);
            }
            if (options.DiceCount < 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidActionOptions, $"Number of dice to roll must be > 0", true, false);
            }
            if (options.DiceCount > stateHelper.Player.GetMaxCountOfDiceCanRoll())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidActionOptions, $"This player can't roll that many dice; they have a max of {stateHelper.Player.GetMaxCountOfDiceCanRoll()} currently.", true, false);
            }

            // Roll the dice!
            m_state.CurrentTurnState.Rolls++;
            m_state.CurrentTurnState.DiceResults.Clear();
            int sum = 0;
            for (int i = 0; i < options.DiceCount; i++)
            {
                int result = m_random.RandomInt(1, 6);
                sum += result;
                m_state.CurrentTurnState.DiceResults.Add(result);
            }

            // Create an update
            GamePlayer p = stateHelper.Player.GetPlayer();
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.RollDiceResult, $"Player {p.Name} rolled {sum}.", p.PlayerIndex,
                new RollDiceDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, PlayerIndex = p.PlayerIndex }));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            if (options.AutoCommitResult)
            {
                HandleDiceRollCommitAction(log, GameAction<object>.CreateCommitDiceResultAction(), stateHelper);
            }
        }

        private void HandleDiceRollCommitAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // This doesn't have options, so there's no need to validate them.

            // Make sure we are in a valid state to do this.
            if(stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player has already committed the dice result.", true);
            }
            if(m_state.CurrentTurnState.DiceResults.Count == 0 || m_state.CurrentTurnState.Rolls == 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player hasn't rolled the dice yet, so they can't commit the results.", true);
            }
            if(m_state.CurrentTurnState.SpecialActions == null || m_state.CurrentTurnState.SpecialActions.Count != 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.PendingSpecialActivations, $"The player has pending special activations that need to be resolved.", true);
            }

            // Commit the dice result.
            m_state.CurrentTurnState.HasCommitedDiceResult = true;

            // Format a nice string.
            string rollStr = "[";
            bool first = true;
            foreach(int r in m_state.CurrentTurnState.DiceResults)
            {
                if(!first)
                {
                    rollStr += ",";
                }
                first = false;
                rollStr += r;
            }
            rollStr += "]";

            GamePlayer p = stateHelper.Player.GetPlayer();
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.CommitDiceResults, $"Player {p.Name} committed their roll of {rollStr} after {m_state.CurrentTurnState.Rolls} rolls.", p.PlayerIndex,
                new CommitDiceResultsDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, PlayerIndex = p.PlayerIndex, Rolls = m_state.CurrentTurnState.Rolls }));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            // Let the earn income logic run. If there are activations returned, there are actions the player needs to decided on.
            List<GameActionType> specialActions = EarnIncome(log, stateHelper);
            m_state.CurrentTurnState.SpecialActions = specialActions;
        }

        private void HandleBuildAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // Try to get the options
            BuildBuildingOptions options = null;
            try
            {
                if (action.Options is JObject obj)
                {
                    options = obj.ToObject<BuildBuildingOptions>();
                }
            }
            catch (Exception e)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidActionOptions, $"Failed to parse build options: {e.Message}", true);
            }
            if (!stateHelper.Marketplace.ValidateBuildingIndex(options.BuildingIndex))
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidActionOptions, $"Invalid building index set in build command.", true);
            }
            
            // Validate the player is in a state where they can build this building.
            if(!stateHelper.Player.CanBuildBuilding(options.BuildingIndex))
            {
                if(stateHelper.Player.CanAffordBuilding(options.BuildingIndex))
                {
                    throw GameErrorException.Create(m_state, ErrorTypes.NotEnoughFunds, $"The player doesn't have enough coins to build the requested building type.", true);
                }
                else if(stateHelper.Player.HasReachedBuildingBuiltLimit(options.BuildingIndex))
                {
                    throw GameErrorException.Create(m_state, ErrorTypes.PlayerMaxBuildingLimitReached, $"The player has reached the per player building limit.", true);
                }
                else
                {
                    throw GameErrorException.Create(m_state, ErrorTypes.NotAvailableInMarketplace, $"The requested building isn't currently available in the marketplace.", true);
                }
            }

            // Make the transaction.

            // Take the player's coins.
            GamePlayer p = stateHelper.Player.GetPlayer();
            p.Coins -= stateHelper.BuildingRules[options.BuildingIndex].GetBuildCost();

            // Add the building to the player.
            p.OwnedBuildings[options.BuildingIndex]++;

            // Remove an instance of it from the marketplace
            m_state.Market.AvailableBuildable[options.BuildingIndex]--;

            // Update the current turn state.
            m_state.CurrentTurnState.HasBougthBuilding = true;

            // Create an update
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.BuildBuilding, $"Player {p.Name} built a {stateHelper.BuildingRules[options.BuildingIndex].GetName()}.", p.PlayerIndex,
                new BuildBuildingDetails() { PlayerIndex = p.PlayerIndex, BuildingIndex = options.BuildingIndex }));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            if (options.AutoEndTurn)
            {                
                HandleEndTurnAction(log, GameAction<object>.CreateEndTurnAction(), stateHelper);
            }
        }

        private void HandleEndTurnAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // This doesn't have options, so there's no need to validate them.

            // Make sure we are in a valid state to do this.
            if (!stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player has not committed a dice roll yet.", true);
            }
            if (m_state.CurrentTurnState.DiceResults.Count == 0 || m_state.CurrentTurnState.Rolls == 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player hasn't rolled the dice yet, so they can't end their turn.", true);
            }

            // Note: It's ok to end the turn without buying.

            // End the turn.
            m_state.CurrentTurnState.HasEndedTurn = true;

            // Log it.
            GamePlayer p = stateHelper.Player.GetPlayer();
            log.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.EndTurn, $"Player {p.Name} has ended their turn.", p.PlayerIndex, null));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);
        }

        private void HandleSpecialAction(List<GameLog> log, GameAction<object> action, StateHelper stateHelper)
        {
            // Validate
            if(!stateHelper.CurrentTurn.HasPendingSpecialActions())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"HandleSpecialAction was called but there were no special actions.", false, true);
            }
            if(stateHelper.GetState().CurrentTurnState.SpecialActions[0] != action.Action)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"HandleSpecialAction was called with action {action.Action.ToString()} but it's not the top action on the list.", false, true);
            }

            // Find the action handler 
            BuildingActivationBase act = BuildingActivationBase.GetActivation(action.Action);
            if(act == null)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"{action.Action.ToString()} was requested as a special action, but there was no handler for it.", false, true);
            }

            // Let the activation handle the player input.
            act.PlayerAction(log, action, stateHelper);

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            // If the action was successful, remove it from our list.
            stateHelper.GetState().CurrentTurnState.SpecialActions.RemoveAt(0);
        }

        #endregion

        #region Helpers

        private List<GameActionType> EarnIncome(List<GameLog> log, StateHelper stateHelper)
        {
            // Validate state.
            if(!stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, "Income tried to be earned when a dice result wasn't committed.", false);
            }
            if(m_state.CurrentTurnState.Rolls == 0 || m_state.CurrentTurnState.DiceResults.Count == 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, "Income tried to be earned when there were no dice results.", false);
            }
            if(m_state.CurrentTurnState.SpecialActions.Count != 0)
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, "Income can't be earned while there are pending special actions.", false);
            }

            // 
            // REDS
            //
            // First, starting with the active player, in reverse order we need to settle red cards.
            // Each player in reverse order should get the full amount from the player for all their red cards before moving on.
            // Red cards don't activate on the current player.

            // Start with the current player - 1;
            int playerIndex = m_state.CurrentTurnState.PlayerIndex - 1;
            while(true)
            {
                // If we roll under, go back to the highest player.
                if(playerIndex < 0)
                {
                    playerIndex = m_state.Players.Count - 1;
                }

                // When we get back to the current player break. We don't need to execute reds on the current player.
                if(playerIndex == m_state.CurrentTurnState.PlayerIndex)
                {
                    break;
                }

                // Execute all red buildings for this player.
                ExecuteBuildingColorIncomeForPlayer(log, stateHelper, playerIndex, EstablishmentColor.Red);

                // Move to the next.
                playerIndex--;
            }

            //
            // BLUES
            //
            // Next, settle any blue cards from any players.
            // Player order doesn't matter.
            for(playerIndex = 0; playerIndex < m_state.Players.Count; playerIndex++)
            {
                ExecuteBuildingColorIncomeForPlayer(log, stateHelper, playerIndex, EstablishmentColor.Blue);
            }

            //
            // GREENS
            //
            // Green cards only execute on the active player's turn
            ExecuteBuildingColorIncomeForPlayer(log, stateHelper, m_state.CurrentTurnState.PlayerIndex, EstablishmentColor.Green);

            //
            // PURPLE
            //
            // Purple cards only execute on the active player's turn.
            // Purple cards may result in actions we need to ask the player about.
            List<GameActionType> specialActions = ExecuteBuildingColorIncomeForPlayer(log, stateHelper, m_state.CurrentTurnState.PlayerIndex, EstablishmentColor.Purple);

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            // Return any activations
            return specialActions;
        }

        private List<GameActionType> ExecuteBuildingColorIncomeForPlayer(List<GameLog> log, StateHelper stateHelper, int playerIndex, EstablishmentColor color)
        {
            List<GameActionType> newSpecialActions = new List<GameActionType>();

            // Get the sum of the roll.
            int diceSum = 0;
            foreach (int r in m_state.CurrentTurnState.DiceResults)
            {
                diceSum += r;
            }

            // Figure out if this player is the current player.
            bool isActivePlayer = playerIndex == m_state.CurrentTurnState.PlayerIndex;

            // Look for any of their buildings that activate.
            for (int buildingIndex = 0; buildingIndex < stateHelper.BuildingRules.GetCountOfUniqueTypes(); buildingIndex++)
            {
                // Check if the building activates.
                BuildingBase building = stateHelper.BuildingRules[buildingIndex];
                if (building.GetEstablishmentColor() == color && building.IsDiceInRange(diceSum))
                {
                    // Active the card if this is the active player or if the card activates on other player's turns.
                    if((isActivePlayer || building.ActivatesOnOtherPlayersTurns()))
                    {
                        // Execute for every building the player has.
                        int built = stateHelper.Player.GetBuiltCount(buildingIndex, playerIndex);
                        for (int i = 0; i < built; i++)
                        {
                            // This building should activate.
                            BuildingActivationBase activation = building.GetActivation();
                            activation.Activate(log, m_state, stateHelper, buildingIndex, playerIndex);

                            // If the building has a special activation, we need to add it to the list so the player can resolve it.
                            GameActionType? type = activation.GetAction();
                            if (type.HasValue)
                            {
                                newSpecialActions.Add(type.Value);
                            }
                            ThrowIfInvalidState(stateHelper);
                        }                     
                    }
                }
            }
            return newSpecialActions;
        }

        private void ThrowIfInvalidState(StateHelper stateHelper)
        {
            string err = stateHelper.Validate();
            if(!String.IsNullOrWhiteSpace(err))
            {
                throw GameErrorException.Create(m_state, ErrorTypes.InvalidState, $"The game is now in an invalid state. Error: {err}", false, true);
            }
        }

        #endregion
    }
}
