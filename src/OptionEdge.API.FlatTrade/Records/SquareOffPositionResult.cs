﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class SquareOffPositionResult : BaseResponseResult
    {
        [JsonPropertyName("nestOrderNumber")]
        public string OrderNumber { get; set; }
    }
}
