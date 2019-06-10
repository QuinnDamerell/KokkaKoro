using GameCommon.Buildings;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommon.Protocol.GameUpdateDetails
{
    // Indicates that a coin payment was done.
    public class CoinPaymentDetials
    {
        // The player index of who paid the coins.
        public int PlayerIndexTakenFrom;

        // The player index of who was given the coins.
        public int PlayerIndexPaidTo;

        // How much was paid.
        public int Payment;

        // From which building
        public int BuildingIndex;
    }
}
