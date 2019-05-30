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
        // Sets the user name for the connection.
        [EnumMember(Value = "SetUserName")]
        SetUserName = 0,

        // Returns a list of all games currently known of.
        [EnumMember(Value = "ListGames")]
        ListGames = 1,

        // Retuns a list of all bots we know of.
        [EnumMember(Value = "ListBots")]
        ListBots = 2,

        // Creates a new game with the CreateGameOptions class.
        [EnumMember(Value = "CreateGame")]
        CreateGame = 3,

        // Adds a bot to a game
        [EnumMember(Value = "AddBot")]
        AddBot = 4,

        // Joins the current client as a live player.
        [EnumMember(Value = "JoinGame")]
        JoinGame = 5,

        // Starts the given game.
        [EnumMember(Value = "StartGame")]
        StartGame = 6,

        [EnumMember(Value = "SpecateGame")]
        SpecateGame = 7,
    }

    public class KokkaKoroRequest<T>
    {
        // Indicates which command we are sending.
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroCommands Command;

        // A unique request id that can be used to match a response.
        public int RequestId;

        // The object that will hold options depeding on the command type.
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
