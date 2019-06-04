using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GameCommon.Protocol
{
    public class GameActionResponse
    {
        // Indicates if the action was accepted or not.
        public bool Accepted;

        // If the action wasn't accepted, this will give details on why.
        public GameError Error;

        //
        // Helpers
        //
        public static GameActionResponse CreateError(GameError error)
        {
            return new GameActionResponse() { Accepted = false, Error = error };
        }

        public static GameActionResponse CreateSuccess()
        {
            return new GameActionResponse() { Accepted = true, Error = null };
        }
    }
}
