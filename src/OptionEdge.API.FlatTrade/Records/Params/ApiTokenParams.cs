using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class ApiTokenParams
    {
        [JsonProperty("api_key")]
        public string ApiKey;
        [JsonProperty("request_code")]
        public string RequestCode;
        [JsonProperty("api_secret")]
        public string ApiSecret;
    }
}
