using GameCommon.BuildingActivations;
using GameCommon.Protocol;
using GameCommon.StateHelpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon
{
    public enum GameMode
    {
        Base
    }

    public class TurnState
    {
        /// <summary>
        /// The index into the Players list of the current player.
        /// </summary>
        public int PlayerIndex;

        /// <summary>
        /// How many rolls the current user has preformed.
        /// </summary>
        public int Rolls;

        /// <summary>
        /// The results of the dice that have been rolled, assuming they have been rolled.
        /// </summary>
        public List<int> DiceResults = new List<int>();

        /// <summary>
        /// If anything in this list exits, there are special actions the player must resolved.
        /// The actions MUST BE RESOLVED BEFORE ANY OTHER TURN ACTIONS TAKE PLACE.
        /// The actions MUST BE RESOLVED IN THE LIST ORDER.
        /// </summary>
        public List<GameActionType> SpecialActions = new List<GameActionType>();

        /// <summary>
        /// Indicates if the player has committed to the dice result.
        /// </summary>
        public bool HasCommitedDiceResult;

        /// <summary>
        /// Indicates if the player has bought a building on this turn or not.
        /// </summary>
        public bool HasBougthBuilding;

        /// <summary>
        /// Indicates if the player has ended this turn.
        /// </summary>
        public bool HasEndedTurn;

        /// <summary>
        /// Indicates which round of the game we are on.
        /// </summary>
        public int RoundNumber;

        /// <summary>
        /// Indicates if the game has ended.
        /// </summary>
        public bool HasGameEnded = false;

        /// <summary>
        /// Indicates if the game has started.
        /// </summary>
        public bool HasGameStarted = false;

        // 
        // Helpers
        //
        public TurnState()
        {
            RoundNumber = 0;
            Clear(0);
        }

        public void Clear(int newPlayerIndex)
        {
            PlayerIndex = newPlayerIndex;
            Rolls = 0;
            DiceResults.Clear();
            HasBougthBuilding = false;
            HasCommitedDiceResult = false;
            HasEndedTurn = false;
            SpecialActions.Clear();
        }
    }

    public class GameState
    {
        /// <summary>
        /// This is the game version the local client is running.
        /// The game version will be updated whenever there are breaking changes to the cards or rules.
        /// </summary>
        [JsonIgnore]
        public static int GameVersion = 1;

        /// <summary>
        /// This is the version of the game the remote sever is running.
        /// </summary>
        public int RemoteGameVersion = GameVersion;

        /// <summary>
        /// The game mode being played.
        /// </summary>
        public GameMode Mode;

        /// <summary>
        /// A list of players in the game.
        /// The players are ordered by turn order.
        /// </summary>
        public List<GamePlayer> Players = new List<GamePlayer>();

        /// <summary>
        /// The current state of the current player's turn.
        /// </summary>
        public TurnState CurrentTurnState = new TurnState();

        /// <summary>
        /// The market is the set of cards that are currently available for purchase.
        /// </summary>
        public Marketplace Market;

        /// <summary>
        /// Returns a state helper where questions are answered from the perspective of the given username.
        /// </summary>
        /// <param name="perspectiveUserName"></param>
        /// <returns></returns>
        public StateHelper GetStateHelper(string perspectiveUserName)
        {
            return new StateHelper(this, perspectiveUserName);
        }
    }
}
