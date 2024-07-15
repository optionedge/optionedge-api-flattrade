using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class WebsocketAccessTokenParams
    {
        [JsonProperty("loginType")]
        public string LoginType;
    }
}
