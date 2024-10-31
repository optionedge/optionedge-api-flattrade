using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class LimitsResult : BaseResponseResult
    {
        [JsonPropertyName("request_time")]
        public string RequestTime { get; set; }

        /// <summary>
        /// 	Cash Margin available
        /// </summary>
        [JsonPropertyName("cash")]
        public string Cash { get; set; }

        /// <summary>
        /// 	Total Amount transferred using Payins today
        /// </summary>
        [JsonPropertyName("payin")]
        public string Payin { get; set; }

        /// <summary>
        /// 	Total amount requested for withdrawal today
        /// </summary>
        [JsonPropertyName("payout")]
        public string Payout { get; set; }

        // --

        /// <summary>
        /// 	Prevalued Collateral Amount
        /// </summary>
        [JsonPropertyName("brkcollamt")]
        public string PrevaluedCollateralAmount { get; set; }

        /// <summary>
        /// 	Uncleared Cash (Payin through cheques)
        /// </summary>
        [JsonPropertyName("unclearedcash")]
        public string UnclearedCash { get; set; }

        /// <summary>
        /// 	Additional leverage amount / Amount added to handle system errors - by broker.
        /// </summary>
        [JsonPropertyName("daycash")]
        public string Daycash { get; set; }


        /// ---
        /// 
        [JsonPropertyName("turnoverlmt")]
        public string TurnoverLimit { get; set; }

        [JsonPropertyName("pendordvallmt")]
        public string PendingOrderValueALimit { get; set; }

        [JsonPropertyName("turnover")]
        public string Turnover { get; set; }

        [JsonPropertyName("pendordval")]
        public string PendingOrderValue { get; set; }

        [JsonPropertyName("marginused")]
        public string MarginUsed { get; set; }

        [JsonPropertyName("mtomcurper")]
        public string MTOMCurrentPercent { get; set; }

        [JsonPropertyName("urmtom")]
        public string UnrealizedMTM { get; set; }

        [JsonPropertyName("grexpo")]
        public string GrossExposure { get; set; }
        [JsonPropertyName("uzpnl_e_i")]
        public string CurrentUnrealizedMTOMEquityIntraday { get; set; }

        [JsonPropertyName("uzpnl_e_m")]
        public string CurrentUnrealizedMTOMEquityMargin { get; set; }

        [JsonPropertyName("uzpnl_e_c")]
        public string CurrentUnrealizedMTOMEquityCashAndCarry { get; set; }

        [JsonPropertyName("peak_mar")]
        public string PeakMargin { get; set; }

        [JsonPropertyName("premium")]
        public string Premium { get; set; }

        [JsonPropertyName("brokerage")]
        public string Brokerage { get; set; }

        [JsonPropertyName("premium_d_m")]
        public string PremiumDay { get; set; }

        [JsonPropertyName("brkage_d_m")]
        public string BrokerageDay { get; set; }
    }
}
