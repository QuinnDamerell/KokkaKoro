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

        // If the action fails, this will indicate if the player should try again.
        // This essentially differences two types of errors, if the error was because of a invalid action choice or if it was anything else.
        public bool CanTryAgain;

        // If the action wasn't accepted this message will indicate why.
        public string ErrorIfFailed;

        //
        // Helpers
        //
        public static SendGameActionResponse CreateError(string errorMessage, bool shouldRetry)
        {
            return new SendGameActionResponse() { Accepted = false, CanTryAgain = shouldRetry, ErrorIfFailed = errorMessage };
        }

        public static SendGameActionResponse CreateSuccess()
        {
            return new SendGameActionResponse() { Accepted = true, CanTryAgain = false, ErrorIfFailed = null };
        }
    }
}
