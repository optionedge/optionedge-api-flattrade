using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    internal class WebsocketSession
    {
        [JsonPropertyName("wsSess")]
        public string SessionId { get; set; }
    }
}
