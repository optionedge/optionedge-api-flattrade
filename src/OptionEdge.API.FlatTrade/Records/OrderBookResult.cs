﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class OrderBookResult : BaseResponseResult
    {
        [JsonPropertyName("Prc")]
        public decimal Price { get; set; }
        [JsonPropertyName("RequestID")]
        public string RequestID { get; set; }

        [JsonPropertyName("Cancelqty")]
        public int CanceledQty { get; set; }
        [JsonPropertyName("discQtyPerc")]
        public string DisclosedQtyPercentage { get; set; }

        [JsonPropertyName("customText")]
        public string CustomText { get; set; }
        [JsonPropertyName("Mktpro")]
        public string MarketWithProtection { get; set; }
        [JsonPropertyName("mpro")]
        public string MarketProtectionFlag { get; set; }
        [JsonPropertyName("Qty")]
        public decimal Qty { get; set; }
        [JsonPropertyName("Unfilledsize")]
        public int UnfilledQty { get; set; }

        [JsonPropertyName("orderAuthStatus")]
        public string OrderAuthStatus { get; set; }

        [JsonPropertyName("Usercomments")]
        public string UserComments { get; set; }
        [JsonPropertyName("ticksize")]
        public decimal TickSize { get; set; }
        [JsonPropertyName("Prctype")]
        public string PriceType { get; set; }
        [JsonPropertyName("Status")]
        public string OrderStatus { get; set; }
        [JsonPropertyName("Minqty")]
        public int MinimumOrderQty { get; set; }

        [JsonPropertyName("orderCriteria")]
        public string OrderCriteria { get; set; }
        [JsonPropertyName("Exseg")]
        public string ExchangeSegment { get; set; }
        [JsonPropertyName("Sym")]
        public string SymbolToken { get; set; }

        [JsonPropertyName("Trsym")]
        public string TradingSymbol { get; set; }
        [JsonPropertyName("multiplier")]
        public int Multiplier { get; set; }
        [JsonPropertyName("ExchOrdID")]
        public string ExchangeOrderNumber { get; set; }
        [JsonPropertyName("ExchConfrmtime")]
        public string ExchangeConfirmationTime { get; set; }
        [JsonPropertyName("Pcode")]
        public string ProductCode { get; set; }

        [JsonPropertyName("SyomOrderId")]
        public string SyomOrderId { get; set; }
        [JsonPropertyName("Dscqty")]
        public decimal DisclosedQty { get; set; }
        [JsonPropertyName("Exchange")]
        public string Exchange { get; set; }
        [JsonPropertyName("Ordvaldate")]
        public string OrderValidity { get; set; }
        [JsonPropertyName("accountId")]
        public string AccountId { get; set; }
        [JsonPropertyName("exchangeuserinfo")]
        public string ExchangeUserInfo { get; set; }
        [JsonPropertyName("Avgprc")]
        public decimal AveragePrice { get; set; }

        [JsonPropertyName("Trgprc")]
        public decimal TriggerPrice { get; set; }
        [JsonPropertyName("Trantype")]
        public string TransactionType { get; set; }
        [JsonPropertyName("bqty")]
        public int LotSize { get; set; }
        [JsonPropertyName("Filledshares")]
        public string FilledQty { get; set; }
        [JsonPropertyName("AlgoCategory")]
        public string AlgoCategory { get; set; }
        [JsonPropertyName("sipindicator")]
        public string SIPIndicator { get; set; }
        [JsonPropertyName("strikePrice")]
        public decimal Strike { get; set; }
        [JsonPropertyName("reporttype")]
        public string ReportType { get; set; }

        [JsonPropertyName("AlgoID")]
        public string AlgoID { get; set; }
        [JsonPropertyName("noMktPro")]
        public string NoMarketProtectionFlag { get; set; }
        [JsonPropertyName("BrokerClient")]
        public string BrokerClient { get; set; }

        [JsonPropertyName("OrderUserMessage")]
        public string OrderUserMessage { get; set; }
        [JsonPropertyName("decprec")]
        public string DecimalPrecision { get; set; }
        [JsonPropertyName("ExpDate")]
        public string Expiry { get; set; }
        [JsonPropertyName("COPercentage")]
        public decimal CoverOrderPercentage { get; set; }

        [JsonPropertyName("marketprotectionpercentage")]
        public string MarketProtectionPercentage { get; set; }
        [JsonPropertyName("Nstordno")]
        public string OrderNumber { get; set; }
        [JsonPropertyName("ExpSsbDate")]
        public string ExpirySSBDate { get; set; }
        [JsonPropertyName("OrderedTime")]
        public string OrderedTime { get; set; }
        [JsonPropertyName("RejReason")]
        public string RejectionReason { get; set; }
        [JsonPropertyName("modifiedBy")]
        public string ModifiedBy { get; set; }
        [JsonPropertyName("Scripname")]
        public string ScripName { get; set; }

        [JsonPropertyName("orderentrytime")]
        public string OrderEntryTime { get; set; }
        [JsonPropertyName("PriceDenomenator")]
        public decimal PriceDenomenator { get; set; }
        [JsonPropertyName("panNo")]
        public string PANNo { get; set; }
        [JsonPropertyName("RefLmtPrice")]
        public decimal ReferenceLimitPriceForCoverOrder { get; set; }
        [JsonPropertyName("PriceNumerator")]
        public decimal PriceNumerator { get; set; }
        [JsonPropertyName("token")]
        public int InstrumentToken { get; set; }
        [JsonPropertyName("ordersource")]
        public string OrderSource { get; set; }
        [JsonPropertyName("Validity")]
        public string Validity { get; set; }
        [JsonPropertyName("GeneralDenomenator")]
        public decimal GeneralDenomenator { get; set; }
        [JsonPropertyName("series")]
        public string Series { get; set; }
        [JsonPropertyName("InstName")]
        public string InstrumentName { get; set; }
        [JsonPropertyName("GeneralNumerator")]
        public decimal GeneralNumerator { get; set; }
        [JsonPropertyName("user")]
        public string UserId { get; set; }
        [JsonPropertyName("remarks")]
        public string Remarks { get; set; }
        [JsonPropertyName("iSinceBOE")]
        public int iSinceBOE { get; set; }
        [JsonPropertyName("defmktproval")]
        public string DefaultMarketProtectionValue { get; set; }
        [JsonPropertyName("optionType")]
        public string OptionType { get; set; }

    }
}
