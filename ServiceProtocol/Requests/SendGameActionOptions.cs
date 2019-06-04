using GameCommon.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceProtocol.Requests
{
    public class SendGameActionOptions
    {
        // Required - The game id your sending the action to.
        public Guid GameId;

        // Required - The game action
        public GameAction<object> Action;
    }
}
