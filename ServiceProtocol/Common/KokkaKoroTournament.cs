using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol.Common
{
    public enum TournamentStatus
    {
        [EnumMember(Value = "Created")]
        Created,

        [EnumMember(Value = "Running")]
        Running,

        [EnumMember(Value = "Complete")]
        Complete,

        [EnumMember(Value = "Error")]
        Error
    }

    public class KokkaKoroTournament
    {
        // The id of the tournament
        public Guid Id;

        // The current status
        [JsonConverter(typeof(StringEnumConverter))]
        public TournamentStatus Status;

        // If the status is error, the message that stopped it.
        public string MessageIfError;

        // The user name that created the tournament
        public string CreatedFor;

        // A reason why it's created.
        public string Reason;
    }
}
