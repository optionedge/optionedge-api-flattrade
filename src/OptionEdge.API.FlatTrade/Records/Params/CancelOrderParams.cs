using Newtonsoft.Json;
using System.Linq;

namespace OptionEdge.API.FlatTrade
{
    public class CancelOrderParams
    {
        [JsonProperty("nestOrderNumber")]
        public string OrderNumber;
    }
}
