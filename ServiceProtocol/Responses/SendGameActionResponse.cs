using GameCommon.Protocol;
using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class SendGameActionResponse
    {
        // The response sent from the game engine.
        public GameActionResponse Response;

        //
        // Helpers
        //

        public static SendGameActionResponse CreateResponse(GameActionResponse response)
        {
            return new SendGameActionResponse() { Response = response };
        }
    }
}
