using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class PlaceOrderResult : BaseResponseResult
    {
        [JsonPropertyName("norenordno")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("request_time")]
        public string ResponseReceivedTime { get; set; }

        
    }
}
