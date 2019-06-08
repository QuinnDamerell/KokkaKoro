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
        LogKeeper m_logKeeper;
        RandomGenerator m_random = new RandomGenerator();
        object m_actionLock = new object();
        int? m_roundLimit = null;

        public GameEngine(List<InitalPlayer> players, GameMode mode, int? roundLimit = null)
        {
            // Create a new logger.
            m_logKeeper = new LogKeeper();

            // Set the round limit.
            m_roundLimit = roundLimit;

            // Build the game initial state.
            SetupGame(players, mode);
        }

        public List<GameLog> GetLogs()
        {
            return m_logKeeper.GetLogs();
        }

        public GameState GetState()
        {
            return m_state;
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
            try
            {
                // If we didn't get a state helper just make one. 
                // Check for a winner shouldn't need the current player to work correctly.
                if (stateHelper == null)
                {
                    stateHelper = m_state.GetStateHelper("");
                }

                // Create an empty state helper to check for a winner.
                winner = stateHelper.Player.CheckForWinner();
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
            int? playerIndex = null;
            if (winner != null)
            {
                playerIndex = winner.PlayerIndex;
            }
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

            // Check to see if the action committed made the player win.
            GamePlayer player = stateHelper.Player.CheckForWinner();
            if (player != null)
            {
                // We have a winner!
                EndGameInternal(actionLog, GameEndReason.PlayerWon, stateHelper);
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
            m_state.Market.ReplenishMarket(m_random, m_state.GetStateHelper(String.Empty));
        }

        private void StartGame(List<GameLog> log, StateHelper stateHelper)
        {
            // Set the flag.
            m_state.CurrentTurnState.HasGameStarted = true;

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

        private void AdvanceToNextPlayer(List<GameLog> actionLog, StateHelper stateHelper)
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

            // Check if the player has the amusement park and rolled doubles, they get to take another turn.
            if (stateHelper.Player.GetsExtraTurn())
            {
                // If so, set them to be the player again.
                newPlayerIndex = m_state.CurrentTurnState.PlayerIndex;

                // Log it
                actionLog.Add(GameLog.CreateGameStateUpdate<object>(m_state, StateUpdateType.ExtraTurn, $"{stateHelper.Player.GetPlayer().Name} rolled double {m_state.CurrentTurnState.DiceResults[0]}s and has an Amusement Park, so they get to take an extra turn!", null));
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
            GamePlayer p = stateHelper.Player.GetPlayer(null);
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

            // Validate things are good.
            ThrowIfInvalidState(stateHelper);
        }

        private void ExecuteBuildingColorIncomeForPlayer(List<GameLog> log, StateHelper stateHelper, int playerIndex, EstablishmentColor color)
        {
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
                            building.GetActivation().Activate(log, m_state, stateHelper, buildingIndex, playerIndex);
                            ThrowIfInvalidState(stateHelper);
                        }

                        // If the player has a shopping mall, they get 1 extra coin per building for any activate cup or bread establishment.
                        if(stateHelper.Player.HasShoppingMall() &&
                            (building.GetEstablishmentProduction() == EstablishmentProduction.Bread || building.GetEstablishmentProduction() == EstablishmentProduction.Cup))
                        {
                            // Give them one coin for each building.
                            stateHelper.Player.GetPlayer().Coins += built;

                            // Log it
                            log.Add(GameLog.CreateGameStateUpdate(m_state, StateUpdateType.EarnIncome, $"{stateHelper.Player.GetPlayer().Name} earned an extra {built} from a {building.GetName()}(s) because they have the shopping mall.",
                                        new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = built, PlayerIndex = playerIndex }));
                        }
                    }
                }
            }            
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
