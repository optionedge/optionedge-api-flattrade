using System;
using System.Collections.Generic;
using System.Text;

namespace OptionEdge.API.FlatTrade.Records
{
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; }
        public string Status { get; set; } = Constants.API_RESPONSE_STATUS_Not_OK;
        public string Message { get; set; }

    }
}
