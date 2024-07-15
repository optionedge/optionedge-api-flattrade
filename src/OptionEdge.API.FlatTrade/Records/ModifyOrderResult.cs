using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class ModifyOrderResult : BaseResponseResult
    {
        [JsonPropertyName("Result")]
        public string OrderNumber { get; set; }
    }
}
