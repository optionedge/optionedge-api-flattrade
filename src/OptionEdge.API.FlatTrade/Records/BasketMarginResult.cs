using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptionEdge.API.FlatTrade.Records
{
    public class BasketMarginResult
    {
        [JsonProperty("stat")]
        public string Status { get; set; }

        [JsonProperty("remarks")]
        public string Remarks { get; set; }

        [JsonProperty("marginused")]
        public decimal MarginUsed { get; set; }

        [JsonProperty("marginusedtrade")]
        public decimal MarginUsedTrade { get; set; }

        [JsonProperty("emsg")]
        public string ErrorMessage { get; set; }
    }
}
