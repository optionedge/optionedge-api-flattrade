using Newtonsoft.Json;
using System.Linq;

namespace OptionEdge.API.FlatTrade.Records
{
    public class OrderHistoryParams : BaseParams
    {
        [JsonProperty("norenordno")]
        public string OrderNumber;       
    }
}
