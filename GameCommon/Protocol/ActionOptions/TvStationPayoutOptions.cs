using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol.ActionOptions
{
    public class TvStationPayoutOptions
    {
        // Indicates which player you wish to take the payout from.
        public int PlayerIndexToTakeFrom;
    }
}
