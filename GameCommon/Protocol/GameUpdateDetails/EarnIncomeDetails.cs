using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    public class EarnIncomeDetails
    {
        // The player index who earned the coins.
        public int PlayerIndex;

        // How much was earned.
        public int Earned;

        // From which building
        public int BuildingIndex;

        //
        // Helpers
        //
        public static GameLog Create(GameState state, string playerName, int playerIndex, string buidlingName, int buildingIndex, int amountEarned, string customMessage = null)
        {
            // Log it
            return GameLog.CreateGameStateUpdate(state, StateUpdateType.EarnIncome, (String.IsNullOrEmpty(customMessage) ?  $"{playerName} earned {amountEarned} from a {buidlingName}" : customMessage),
                        new EarnIncomeDetails() { BuildingIndex = buildingIndex, Earned = amountEarned, PlayerIndex = playerIndex });
        }
    }
}
