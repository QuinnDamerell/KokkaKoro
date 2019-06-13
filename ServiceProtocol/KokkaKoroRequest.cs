using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol
{
    public enum KokkaKoroCommands
    {        
        // Logs the user in for the given connection.
        [EnumMember(Value = "Login")]
        Login = 0,

        // Clients must heartbeat to keep the websocket alive.
        [EnumMember(Value = "Heartbeat")]
        Heartbeat = 1,

        // Returns a list of all games currently known of.
        [EnumMember(Value = "ListGames")]
        ListGames = 2,

        // Returns a list of all bots we know of.
        [EnumMember(Value = "ListBots")]
        ListBots = 3,

        // Creates a new game with the CreateGameOptions class.
        [EnumMember(Value = "CreateGame")]
        CreateGame = 4,

        // Adds a hosted bot to a game.
        [EnumMember(Value = "AddHostedBot")]
        AddHostedBot = 5,

        // Joins the current client as a player.
        [EnumMember(Value = "JoinGame")]
        JoinGame = 6,

        // Starts the given game.
        [EnumMember(Value = "StartGame")]
        StartGame = 7,

        // Sends an action to a game.
        [EnumMember(Value = "SendGameAction")]
        SendGameAction = 8,

        // Returns all of the current game logs for the given game.
        [EnumMember(Value = "GetGameLogs")]
        GetGameLogs = 9,

        // Uploads a new or updated bot
        [EnumMember(Value = "AddOrUpdateBot")]
        AddOrUpdateBot = 10,

        // Creates a new tournament
        [EnumMember(Value = "CreateTournament")]
        CreateTournament = 11,

        // Creates a new tournament
        [EnumMember(Value = "ListTournaments")]
        ListTournaments = 12,
    }

    public class KokkaKoroRequest<T>
    {
        // If the server doesn't hear from the client in this amount of time the socket will be closed.
        [JsonIgnore]
        public static int HeartbeatTimeoutMs = 4000;

        // Indicates which command we are sending.
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroCommands Command;

        // A unique request id that can be used to match a response.
        public int RequestId;

        // The object that will hold options depending on the command type.
        public T CommandOptions;

        //
        // Helper functions that are handy
        //

        private readonly static Random m_idGenerator = new Random();

        public KokkaKoroRequest()
        {
            RequestId = m_idGenerator.Next(10000000, 99999999);
        }
    }
}
