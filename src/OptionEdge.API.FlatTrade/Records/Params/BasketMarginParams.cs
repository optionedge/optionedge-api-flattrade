﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace OptionEdge.API.FlatTrade.Records
{
    public class BasketMarginParams : BaseParams
    {
        [JsonProperty("basketlists")]
        public List<BasketOrderItem> BasketItems { get; set; }

        [JsonProperty("exch")]
        public string Exchange { get; set; }

        [JsonProperty("tsym")]
        public string TradingSymbol { get; set; }

        [JsonProperty("qty")]
        public string Quantity { get; set; }

        [JsonProperty("prc")]
        public string Price { get; set; }

        [JsonProperty("prd")]
        public string Product { get; set; }

        [JsonProperty("trantype")]
        public string TransactionType { get; set; }

        [JsonProperty("prctyp")]
        public string PriceType { get; set; }
    }
}
