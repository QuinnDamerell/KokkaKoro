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
        // The index into the Players list of the current player.
        public int PlayerIndex;

        // How many rolls the current user has preformed.
        public int Rolls;

        // The results of the dice that have been rolled, assuming they have been rolled.
        public List<int> DiceResults = new List<int>();

        // If anything in this list exits, there are special actions the player must resolved.
        // The actions MUST BE RESOLVED BEFORE ANY OTHER TURN ACTIONS TAKE PLACE.
        // The actions MUST BE RESOLVED IN THE LIST ORDER.
        public List<GameActionType> SpecialActions = new List<GameActionType>();

        // Indicates if the player has committed to the dice result.
        public bool HasCommitedDiceResult;

        // Indicates if the player has bought a building on this turn or not.
        public bool HasBougthBuilding;

        // Indicates if the player has ended this turn.
        public bool HasEndedTurn;

        // Indicates which round of the game we are on.
        public int RoundNumber;

        // Indicates if the game has ended.
        public bool HasGameEnded = false;

        // Indicates if the game has started.
        public bool HasGameStarted = false;

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
        // This is the game version the local client is running.
        // The game version will be updated whenever there are breaking changes to the cards or rules.
        [JsonIgnore]
        public static int GameVersion = 1;

        // This is the version of the game the remote sever is running.
        public int RemoteGameVersion = GameVersion;

        // The game mode being played.
        public GameMode Mode;

        // A list of players in the game.
        // The players are ordered by turn order.
        public List<GamePlayer> Players = new List<GamePlayer>();

        // The current state of the current player's turn.
        public TurnState CurrentTurnState = new TurnState();

        // The market is the set of cards that are currently available for purchase.
        public Marketplace Market;

        //
        // Helpers
        //

        // Returns a state helper where questions are answered from the perspective of the given
        // username.
        public StateHelper GetStateHelper(string perspectiveUserName)
        {
            return new StateHelper(this, perspectiveUserName);
        }
    }
}
