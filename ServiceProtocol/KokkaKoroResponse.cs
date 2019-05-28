﻿using Newtonsoft.Json;
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
    }

    public class KokkaKoroResponse<T>
    {
        // Indicates which kind of response this is.
        [JsonConverter(typeof(StringEnumConverter))]
        public KokkaKoroResponseType Type;

        // The client supplied request id for the request.
        public int RequestId;

        // The the data of the message.
        public T Data;

        // If not null, this indicates and descirbes an error that occured.
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
    }
}
