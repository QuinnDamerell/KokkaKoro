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
        // Returns a list of all games currently known of.
        [EnumMember(Value = "ListGames")]
        ListGames   = 0,

        // Creates a new game with the CreateGameOptions class.
        [EnumMember(Value = "CreateGame")]
        CreateGame = 1,

        // Adds a bot to a game
        [EnumMember(Value = "AddBot")]
        AddBot = 2,

        // Joins the current client as a live player.
        [EnumMember(Value = "JoinGame")]
        JoinGame = 2,

        // Starts the given game.
        [EnumMember(Value = "StartGame")]
        StartGame = 3,

        [EnumMember(Value = "SpecateGame")]
        SpecateGame = 4,
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
