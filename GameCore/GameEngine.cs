using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameCommon;
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
        GameState m_state;
        bool m_gameStarted = false;
        LogKeeper m_logKeeper;
        RandomGenerator m_random = new RandomGenerator();
        object m_actionLock = new object();

        public GameEngine(List<InitalPlayer> players, GameMode mode)
        {
            // Create a new logger.
            m_logKeeper = new LogKeeper();

            // Build the game initial state.
            SetupGame(players, mode);
        }

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
                catch (GameError e)
                {
                    // If the call throws an action exception, there was something wrong with the action. 

                    // Add the error to the log.
                    actionLog.Add(GameLog.CreateError(e));

                    // Return the error and the action log.                
                    return (GameActionResponse.CreateError(e), actionLog);
                }
                catch (Exception e)
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
        }

        public (GameActionResponse, List<GameLog>) EndGame(GameEndReason reason)
        {
            // Create a action log.
            List<GameLog> actionLog = new List<GameLog>();

            // End the game.
            try
            {
                EndGameInternal(actionLog, reason);
            }
            catch (GameError e)
            {
                // Add the error to the log.
                actionLog.Add(GameLog.CreateError(e));
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
                throw GameError.Create(m_state, ErrorTypes.GameEnded, $"The game has already been ended.", false);
            }
            
            // End the game by setting the game ended flag.
            m_state.CurrentTurnState.HasGameEnded = true;

            // Now try to find if there was a winner.
            // We try catch this to make sure we always send the end message.
            GamePlayer winner = null;
            int? playerIndex = null;
            try
            {
                // If we didn't get a state helper just make one. 
                // Check for a winner shouldn't need the current player to work correctly.
                if (stateHelper == null)
                {
                    stateHelper = m_state.GetStateHelper("");
                }

                // Create an empty state helper to check for a winner.
                (playerIndex, winner) = stateHelper.Player.CheckForWinner();
            }
            catch (GameError e)
            {
                // Add the error to the log.
                actionLog.Add(GameLog.CreateError(e));
            }
            catch (Exception e)
            {
                // Create an error and add it to the log.
                GameError err = GameError.Create(m_state, ErrorTypes.Unknown, $"An exception was thrown while ending the game. {e.Message}", false);
                actionLog.Add(GameLog.CreateError(err));
            }

            // Create the end game message
            actionLog.Add(GameLog.CreateGameStateUpdate<GameEndDetails>(m_state, StateUpdateType.GameEnd, $"The Game is over because {reason.ToString()}! {(winner == null ? "There was no winner!" : $"{winner.Name} won!")}",
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
                case GameActionType.BuildBuilding:
                    HandleBuildAction(actionLog, action, stateHelper);
                    break;
                case GameActionType.EndTurn:
                    HandleEndTurnAction(actionLog, action, stateHelper);
                    break;
                default:
                    throw GameError.Create(m_state, ErrorTypes.UknownAction, $"Unknown action type. {action.Action}", true);
            }

            // Check if the turn is over.
            if(stateHelper.CurrentTurn.HasEndedTurn())
            {
                AdvanceToNextPlayer(stateHelper);
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
            foreach (InitalPlayer p in players)
            {
                // Create a player.
                GamePlayer gamePlayer = new GamePlayer() { Name = p.FriendlyName, UserName = p.UserName, Coins = 3 };

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
            }

            // Adding building to the marketplace
            m_state.Market.ReplenishMarket(m_random, m_state.GetStateHelper(String.Empty));
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
                throw GameError.Create(m_state, ErrorTypes.PlayerUserNameNotFound, $"`{stateHelper.Player.GetPlayerUserName()}` user name wasn't found in this game.", false);
            }
            if(!stateHelper.CurrentTurn.IsMyTurn())
            {
                throw GameError.Create(m_state, ErrorTypes.NotPlayersTurn, $"`{stateHelper.Player.GetPlayerUserName()}` tried to send a action when it's not their turn.", false);
            }

            // Next, make sure we have an action.
            if (action == null)
            {
                throw GameError.Create(m_state, ErrorTypes.Unknown, $"No action object was sent", false);
            }

            // Last, make sure the game hasn't ended.
            if(stateHelper.CurrentTurn.HasGameEnded())
            {
                throw GameError.Create(m_state, ErrorTypes.GameEnded, $"`{stateHelper.Player.GetPlayerUserName()}` can't take an action because the game has ended.", false);
            }
        }

        private void AdvanceToNextPlayer(StateHelper stateHelper)
        {
            if(m_state.CurrentTurnState.Rolls == 0 || m_state.CurrentTurnState.DiceResults.Count == 0 || !stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidState, $"The turn can't be advanced because the current turn hasn't rolled yet.", true);
            }
            if (!stateHelper.CurrentTurn.HasEndedTurn())
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidState, $"The current turn isn't over, so we can't go to the next player.", false);
            }

            // Find the next player.
            int newPlayerIndex = m_state.CurrentTurnState.PlayerIndex;
            newPlayerIndex++;
            if(newPlayerIndex >= m_state.Players.Count)
            {
                newPlayerIndex = 0;
            }

            // And finally reset the current turn and set the new player.
            m_state.CurrentTurnState.Clear(newPlayerIndex);

            // It's also super important to update the state helper with the new user perspective.
            stateHelper.SetPerspectiveUserName(stateHelper.Player.GetPlayerUserName(newPlayerIndex));
        }

        private void BuildPlayerActionRequest(List<GameLog> log, StateHelper stateHelper)
        {
            // Based on the current state, build a list of possible actions.
            List<GameActionType> actions = stateHelper.CurrentTurn.GetPossibleActions();

            if (actions.Count == 0)
            {
                actions = stateHelper.CurrentTurn.GetPossibleActions();
                throw GameError.Create(m_state, ErrorTypes.InvalidState, $"There are no possible actions for the current player.", false);
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
                int result = m_random.RandomInt(1, 6);
                sum += result;
                m_state.CurrentTurnState.DiceResults.Add(result);
            }  

            // Create an update
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.RollDiceResult, $"Player {stateHelper.Player.GetPlayerName()} rolled {sum}.",
                new RollDiceDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, PlayerIndex = stateHelper.Player.GetPlayerIndex() }));

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
                throw GameError.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player has already committed the dice result.", true);
            }
            if(m_state.CurrentTurnState.DiceResults.Count == 0 || m_state.CurrentTurnState.Rolls == 0)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player hasn't rolled the dice yet, so they can't commit the results.", true);
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

            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.CommitDiceResults, $"Player {stateHelper.Player.GetPlayerName()} committed their roll of {rollStr} after {m_state.CurrentTurnState.Rolls} rolls.",
                new CommitDiceResultsDetails() { DiceResults = m_state.CurrentTurnState.DiceResults, PlayerIndex = stateHelper.Player.GetPlayerIndex(), Rolls = m_state.CurrentTurnState.Rolls }));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);

            // Let the earn income logic run
            EarnIncome(log, stateHelper); 
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
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Failed to parse build options: {e.Message}", true);
            }
            if (!stateHelper.Marketplace.ValidateBuildingIndex(options.BuildingIndex))
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidActionOptions, $"Invalid building index set in build command.", true);
            }
            
            // Validate the player is in a state where they can build this building.
            if(!stateHelper.Player.CanBuildBuilding(options.BuildingIndex))
            {
                if(stateHelper.Player.CanAffordBuilding(options.BuildingIndex))
                {
                    throw GameError.Create(m_state, ErrorTypes.NotEnoughFunds, $"The player doesn't have enough coins to build the requested building type.", true);
                }
                else if(stateHelper.Player.HasReachedPerPlayerBuildingLimit(options.BuildingIndex))
                {
                    throw GameError.Create(m_state, ErrorTypes.PlayerMaxBuildingLimitReached, $"The player has reached the per player building limit.", true);
                }
                else
                {
                    throw GameError.Create(m_state, ErrorTypes.NotAvailableInMarketplace, $"The requested building isn't currently available in the marketplace.", true);
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
            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.BuildBuilding, $"Player {stateHelper.Player.GetPlayerName()} built a {stateHelper.BuildingRules[options.BuildingIndex].GetName()}.",
                new BuildBuildingDetails() { PlayerIndex = stateHelper.Player.GetPlayerIndex(), BuildingIndex = options.BuildingIndex }));

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
                throw GameError.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player has not committed a dice roll yet.", true);
            }
            if (m_state.CurrentTurnState.DiceResults.Count == 0 || m_state.CurrentTurnState.Rolls == 0)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidStateToTakeAction, $"The player hasn't rolled the dice yet, so they can't end their turn.", true);
            }

            // Note: It's ok to end the turn without buying.

            // End the turn.
            m_state.CurrentTurnState.HasEndedTurn = true;

            // Log it.
            log.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.EndTurn, $"Player {stateHelper.Player.GetPlayerName()} has ended their turn.", null));

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);
        }

        #endregion

        #region Helpers

        private void EarnIncome(List<GameLog> log, StateHelper stateHelper)
        {
            // Validate state.
            if(!stateHelper.CurrentTurn.HasCommittedToDiceResult())
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidState, "Income tried to be earned when a dice result wasn't committed.", false);
            }
            if(m_state.CurrentTurnState.Rolls == 0 || m_state.CurrentTurnState.DiceResults.Count == 0)
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidState, "Income tried to be earned when there were no dice results.", false);
            }

            // Get the sum of the roll.
            int diceSum = 0;
            foreach(int r in m_state.CurrentTurnState.DiceResults)
            {
                diceSum += r;
            }

            // First, we must resolve red buildings.
            //for (int b = 0; b < stateHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            //{
            //    BuildingBase building = stateHelper.BuildingRules[b];
            //    if (building.GetEstablishmentColor() == EstablishmentColor.Red)
            //    {
            //        // Check to see if it activated.
            //        if (building.IsDiceInRange(diceSum))
            //        {
            //            ExecuteBuildingIncome(log, stateHelper, b);
            //        }
            //    }
            //}

            // Now build and green.
            for (int b = 0; b < stateHelper.BuildingRules.GetCountOfUniqueTypes(); b++)
            {
                BuildingBase building = stateHelper.BuildingRules[b];
                EstablishmentColor color = building.GetEstablishmentColor();
                if (color == EstablishmentColor.Green || color == EstablishmentColor.Blue)
                {
                    // Check to see if it activated.
                    if (building.IsDiceInRange(diceSum))
                    {
                        ExecuteBuildingIncome(log, stateHelper, b);
                    }
                }
            }

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);
        }


        private void ExecuteBuildingIncome(List<GameLog> log, StateHelper stateHelper, int buildingIndex)
        {
            if(!stateHelper.Marketplace.ValidateBuildingIndex(buildingIndex))
            {
                throw GameError.Create(m_state, ErrorTypes.InvalidState, "Tried to execute building income on a building index out of range.", false);
            }

            BuildingBase building = stateHelper.BuildingRules[buildingIndex];
            switch(building.GetEstablishmentColor())
            {
                case EstablishmentColor.Blue:
                    {
                        // Blue buildings earn income on anyone's turn. So apply the building to anyone who has it.
                        foreach (GamePlayer player in m_state.Players)
                        {
                            int coinsEarned = stateHelper.Player.GetIncomeOnAnyonesTurn(buildingIndex, player.UserName);

                            // If they earned coins, report it.
                            if (coinsEarned != 0)
                            {
                                player.Coins += coinsEarned;
                                log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.EarnIncome, $"{player.Name} earned {coinsEarned} from {player.OwnedBuildings[buildingIndex]} {building.GetName()}(s)",
                                            new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = coinsEarned, PlayerIndex = stateHelper.Player.GetPlayerIndex(player.UserName) }));
                            }
                        }
                        break;
                    }
                case EstablishmentColor.Green:
                    {
                        // Green buildings only earn on the player's turn.
                        GamePlayer player = stateHelper.Player.GetPlayer(stateHelper.CurrentTurn.GetActiveTurnPlayerUserName());

                        // Check if the player earns anything.
                        int coinsEarned = stateHelper.Player.GetIncomeOnMyTurn(buildingIndex, player.UserName);

                        // If they earned coins, report it.
                        if (coinsEarned != 0)
                        {
                            player.Coins += coinsEarned;
                            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.EarnIncome, $"{player.Name} earned {coinsEarned} from {player.OwnedBuildings[buildingIndex]} {building.GetName()}(s)",
                                        new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = coinsEarned, PlayerIndex = stateHelper.Player.GetPlayerIndex(player.UserName) }));;
                        }
                        break;
                    }
                case EstablishmentColor.Red:
                    break;
                case EstablishmentColor.Purple:
                    break;
                default:
                    throw GameError.Create(m_state, ErrorTypes.InvalidState, "Tried to execute building income on a building with an unknown color.", false);                    
            }

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);
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

        #endregion
    }
}
