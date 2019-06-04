using ServiceProtocol.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Responses
{
    public class SendGameActionResponse
    {
        // Indicates if the action was accepted or not.
        public bool Accepted;

        // If the action fails, this will indicate if the action was attempted to be
        // taken on their turn or not.
        public bool WasPlayerTurn;

        // If the action wasn't accepted this message will indicate why.
        public string ErrorIfFailed;
    }
}
