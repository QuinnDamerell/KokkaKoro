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

        // Returns a list of all games currently known of.
        [EnumMember(Value = "ListGames")]
        ListGames = 1,

        // Returns a list of all bots we know of.
        [EnumMember(Value = "ListBots")]
        ListBots = 2,

        // Creates a new game with the CreateGameOptions class.
        [EnumMember(Value = "CreateGame")]
        CreateGame = 3,

        // Adds a hosted bot to a game.
        [EnumMember(Value = "AddHostedBot")]
        AddHostedBot = 4,

        // Joins the current client as a player.
        [EnumMember(Value = "JoinGame")]
        JoinGame = 5,

        // Starts the given game.
        [EnumMember(Value = "StartGame")]
        StartGame = 6,

        // Sends an action to a game.
        [EnumMember(Value = "SendGameAction")]
        SendGameAction = 7,

        [EnumMember(Value = "SpecateGame")]
        SpecateGame = 8
    }

    public class KokkaKoroRequest<T>
    {
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
