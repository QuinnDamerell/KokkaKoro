using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceProtocol
{
    public enum KokkaKoroResponseType
    {
        // Indicates this is a response to the request sent.
        [EnumMember(Value = "RequestResult")]
        RequestResult = 0,

        //  Indicates this is a non-invoked game update.
        [EnumMember(Value = "GameUpdate")]
        GameUpdate = 1,

        //  Indicates this is a non-invoked game action is requested.
        [EnumMember(Value = "GameActionRequest")]
        GameActionRequest = 1,
    }

    public class KokkaKoroResponse<T>
    {
        // Indicates which kind of response this is.
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroResponseType Type;

        // The client supplied request id for the request.
        public int RequestId;

        // The data of the message.
        public T Data;

        // If not null, this indicates and describes an error that occurred.
        public string Error;

        //
        // Helpers that are handy
        //

        public static KokkaKoroResponse<T> CreateError(string error)
        {
            return new KokkaKoroResponse<T>{ Type = KokkaKoroResponseType.RequestResult, Error = error };
        }

        public static KokkaKoroResponse<T> CreateResult(T obj)
        {
            return new KokkaKoroResponse<T> { Type = KokkaKoroResponseType.RequestResult, Data = obj };
        }

        public static KokkaKoroResponse<T> CreateGameUpdate(T obj)
        {
            return new KokkaKoroResponse<T> { Type = KokkaKoroResponseType.GameUpdate, Data = obj };
        }

        public static KokkaKoroResponse<T> CreateActionRequest(T obj)
        {
            return new KokkaKoroResponse<T> { Type = KokkaKoroResponseType.GameActionRequest, Data = obj };
        }
    }
}
