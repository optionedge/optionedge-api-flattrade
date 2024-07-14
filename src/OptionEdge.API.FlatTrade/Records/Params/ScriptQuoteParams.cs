using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class ScriptQuoteParams
    {
        [JsonProperty("exch")]
        public string Exchange;

        [JsonProperty("symbol")]
        public int InstrumentToken;
    }
}
