using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class GetQuoteResult : BaseResponseResult
    {
        [JsonPropertyName("exch")]
        public string Exchange { get; set; }

        [JsonPropertyName("tsym")]
        public string TradingSymbol { get; set; }

        [JsonPropertyName("cname")]
        public string CompanyName { get; set; }

        [JsonPropertyName("symname")]
        public string SymbolName{ get; set; }

        [JsonPropertyName("seg")]
        public string Segment { get; set; }

        [JsonPropertyName("instname")]
        public string InstrumentName { get; set; }

        [JsonPropertyName("isin")]
        public string ISIN { get; set; }


        [JsonPropertyName("pp")]
        public string PricePrecision { get; set; }

        [JsonPropertyName("ls")]
        public string LotSize { get; set; }

        [JsonPropertyName("ti")]
        public string TickSize { get; set; }

        [JsonPropertyName("mult")]
        public string Multiplier { get; set; }

        [JsonPropertyName("uc")]
        public string UpperCircuit { get; set; }

        [JsonPropertyName("lc")]
        public string LowerCircuit { get; set; }

        [JsonPropertyName("prcftr_d")]
        public string PriceFactor { get; set; }

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("lp")]
        public string LastTradedPrice { get; set; }

        [JsonPropertyName("h")]
        public string DayHighPrice { get; set; }

        [JsonPropertyName("l")]
        public string DayLowPrice { get; set; }

        [JsonPropertyName("v")]
        public string Volume { get; set; }

        [JsonPropertyName("ltq")]
        public string LastTradedQty { get; set; }

        [JsonPropertyName("ltt")]
        public string LastTradedTime { get; set; }

    }
}
