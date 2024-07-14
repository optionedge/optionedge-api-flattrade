using Newtonsoft.Json;
using System.Linq;

namespace OptionEdge.API.FlatTrade.Records
{
    public class OrderHistoryParams
    {
        [JsonProperty("nestOrderNumber")]
        public string OrderNumber;       
    }
}
