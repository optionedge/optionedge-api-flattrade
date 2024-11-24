using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class GetQuoteParams : BaseParams
    {
       
        [JsonProperty("exch")]
        public string Exchange;


        [JsonProperty("token")]
        public string Token;
    }
}
