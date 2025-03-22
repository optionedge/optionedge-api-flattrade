using Newtonsoft.Json;
using OptionEdge.API.FlatTrade.Records;
using System.Linq;

namespace OptionEdge.API.FlatTrade
{
    public class HistoryDataParams :BaseParams
    {
        [JsonProperty("exch")]
        public string Exchange;

        [JsonProperty("token")]
        public string InstrumentToken;

        /// <summary>
        /// "1", "3", "5", "10", "15", "30", "60", "120"
        /// </summary>
        [JsonProperty("intrv")]
        public string Interval;

        /// <summary>
        /// Unix Timestamp (seconds)
        /// </summary>
        [JsonProperty("st")]
        public string From;

        /// <summary>
        /// Unix Timestamp (seconds)
        /// </summary>
        [JsonProperty("et")]
        public string To;
    }
}
