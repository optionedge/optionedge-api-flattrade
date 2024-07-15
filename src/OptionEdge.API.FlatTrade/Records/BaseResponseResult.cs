using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class BaseResponseResult
    {
        [JsonPropertyName("stat")]
        public string Status { get; set; }
        [JsonPropertyName("emsg")]
        public string ErrorMessage { get; set; }
    }
}
