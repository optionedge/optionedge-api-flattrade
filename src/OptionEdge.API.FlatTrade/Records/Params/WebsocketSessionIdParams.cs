using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class WebsocketSessionIdParams
    {
        [JsonProperty("userId")]
        [JsonPropertyName("userId")]
        public string UserId;
        [JsonProperty("userData")]
        [JsonPropertyName("userData")]
        public string UserData;
    }
}
