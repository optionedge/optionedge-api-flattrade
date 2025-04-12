using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class HistoryDataResult
    {
        [JsonPropertyName("stat")]
        public string Status { get; set; }

        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("into")]
        public decimal Open { get; set; }

        [JsonPropertyName("inth")]
        public decimal High { get; set; }

        [JsonPropertyName("intl")]
        public decimal Low { get; set; }

        [JsonPropertyName("intc")]
        public decimal Close { get; set; }

        [JsonPropertyName("intv")]
        public int VolumeInterval { get; set; }

        [JsonPropertyName("intoi")]
        public int OIInterval { get; set; }

        [JsonPropertyName("oi")]
        public decimal OI { get; set; }

        [JsonPropertyName("v")]
        public decimal Volume { get; set; }
    }
}
