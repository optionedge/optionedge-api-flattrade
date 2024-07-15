using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class OrderHistoryResult : BaseResponseResult
    {
        [JsonPropertyName("exchange")]
        public string Exchange { get; set; }


        [JsonPropertyName("tsym")]
        public string TradingSymbol { get; set; }
        [JsonPropertyName("norenordno")]
        public string OrderNumber { get; set; }

        [JsonPropertyName("prc")]
        public string Price { get; set; }
        [JsonPropertyName("qty")]
        public int Qty { get; set; }

        [JsonPropertyName("prd")]
        public string Product { get; set; }
        [JsonPropertyName("status")]
        public string OrderStatus { get; set; }

        [JsonPropertyName("prctyp")]
        public string PriceType { get; set; }


        /// <summary>
        /// Report Type (fill/complete etc)
        /// </summary>
        [JsonPropertyName("rpt")]
        public string ReportType { get; set; }

        /// <summary>
        /// 	B -> BUY, S -> SELL [transtype should be 'B' or 'S' else reject].
        /// </summary>
        [JsonPropertyName("trantype")]
        public string TransactionType { get; set; }

        [JsonPropertyName("fillshares")]
        public string FilledQty { get; set; }

        [JsonPropertyName("avgprc")]
        public string AveragePrice { get; set; }

        [JsonPropertyName("rejreason")]
        public string RejectionReason { get; set; }

        [JsonPropertyName("exchordid")]
        public string ExchangeOrdeId { get; set; }


        [JsonPropertyName("cancelqty")]
        public string CanceledQty { get; set; }

        /// <summary>
        /// 	Any message Entered during order entry.
        /// </summary>
        [JsonPropertyName("remarks")]
        public string ScripName { get; set; }

        [JsonPropertyName("dscqty")]
        public int DisclosedQty { get; set; }

        [JsonPropertyName("trgprc")]
        public decimal TriggerPrice { get; set; }

        [JsonPropertyName("ret")]
        public string OrderValidity { get; set; }

        [JsonPropertyName("bpprc")]
        public decimal BookProfitPrice { get; set; }


        [JsonPropertyName("blprc")]
        public decimal BookLossPrice { get; set; }

        [JsonPropertyName("trailprc")]
        public decimal TrailingPrice { get; set; }

        [JsonPropertyName("amo")]
        public string AMO { get; set; }

        [JsonPropertyName("pp")]
        public decimal PricePrecision { get; set; }

        [JsonPropertyName("ti")]
        public decimal TickSize { get; set; }

        [JsonPropertyName("ls")]
        public int LotSize { get; set; }

        [JsonPropertyName("token")]
        public int InstrumentToken { get; set; }
    }
}
