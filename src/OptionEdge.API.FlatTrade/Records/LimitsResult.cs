using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class LimitsResult : BaseResponseResult
    {
        [JsonPropertyName("actid")]
        public string AccountId { get; set; }

        [JsonPropertyName("prd")]
        public string ProductName { get; set; }

        [JsonPropertyName("seg")]
        public string Segment { get; set; }

        [JsonPropertyName("exch")]
        public string Exchange { get; set; }
    }
}
