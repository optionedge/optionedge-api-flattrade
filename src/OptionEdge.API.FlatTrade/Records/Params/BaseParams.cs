using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class BaseParams
    {
        [JsonProperty("uid")]
        public string UserId;
        [JsonProperty("actid")]
        public string AccountId;
    }
}
