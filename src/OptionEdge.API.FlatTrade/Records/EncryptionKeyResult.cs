using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    internal class APITokenResult : BaseResponseResult
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("client")]
        public string Client { get; set; }
    }
}
