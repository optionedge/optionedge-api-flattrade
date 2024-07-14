using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace OptionEdge.API.FlatTrade.Records
{
    public class CreateWebsocketConnectionRequest
    {
        
        [DataMember(Name = "actid")]
        public string AccountId { get; set; }


        [DataMember(Name = "t")]
        public string RequestType { get; set; } = "c";


        [DataMember(Name = "uid")]
        public string UserId { get; set; }


        [DataMember(Name = "source")]
        public string Source { get; set; } = "API";


        [DataMember(Name = "susertoken")]
        public string AccessToken { get; set; }
    }
}
