using Newtonsoft.Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class PlaceOrderParams : BaseParams
    {
        /// <summary>
        /// Not in used. Use TradingSymbol instead
        /// </summary>
        [JsonIgnore]
        public int InstrumentToken;

        /// <summary>
        /// NSE / NFO / BSE / MCX
        /// </summary>
        [JsonProperty("exch")]
        public string Exchange;

        /// <summary>
        /// Unique id of contract on which order to be placed. (use url encoding to avoid special char error for symbols like M&M)
        /// </summary>
        [JsonProperty("tsym")]
        public string TradingSymbol;

        [JsonProperty("qty")]
        public string Quantity;

        /// <summary>
        /// Order Price [If prc is junk value other than numbers] "Order price cannot be zero" [if prctyp = 'MKT/ SL-MKT' with price '0' ].
        /// </summary>
        [JsonProperty("prc")]
        public string Price;
        //public bool ShouldSerializePrice()
        //{
        //    // don't serialize the Manager property if an employee is their own manager
        //    return (Price > 0);
        //}

        /// <summary>
        /// 	Only to be sent in case of SL / SL-M order.
        /// </summary>
        [JsonProperty("trgprc")]
        public decimal TriggerPrice;
        public bool ShouldSerializeTriggerPrice()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (TrailingPrice > 0);
        }


        /// <summary>
        /// C - CNC/ M - NRML / H - CO / B - BO / I - MIS / F - MTF
        /// 	
        /// Product name (Select from ‘prarr’ Array provided in User Details response, and if same is allowed for selected, exchange. Show product display name, for user to select, and send corresponding prd in API call)
        /// </summary>
        [JsonProperty("prd")]
        public string ProductCode;

        /// <summary>
        /// B / S
        /// B -> BUY, S -> SELL [transtype should be 'B' or 'S' else reject].
        ///
        /// </summary>
        [JsonProperty("trantype")]
        public string TransactionType;

        /// <summary>
        /// LMT / MKT / SL-LMT / SL-MKT
        /// </summary>
        [JsonProperty("prctyp")]
        public string PriceType;

        /// <summary>
        /// DAY / EOS / IOC
        /// </summary>
        [JsonProperty("ret")]
        public string RetentionType;

        /// <summary>
        /// Any tag by user to mark order.
        /// </summary>
        [JsonProperty("remarks")]
        public string Remarks;

        /// <summary>
        /// API
        /// </summary>
        [JsonProperty("ordersource")]
        public string OrderSource;

        /// <summary>
        /// Book Profit Price applicable only if product is selected as B (Bracket order )
        /// </summary>
        [JsonProperty("bpprc")]
        public decimal BookProfitPrice;
        public bool ShouldSerializeBookProfitPrice()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (BookProfitPrice > 0);
        }

        /// <summary>
        /// Book loss Price applicable only if product is selected as H and B (High Leverage and Bracket order )
        /// </summary>
        [JsonProperty("blprc")]
        public decimal BookLossPrice;
        public bool ShouldSerializeBookLossPrice()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (BookLossPrice > 0);
        }
        /// <summary>
        /// Trailing Price applicable only if product is selected as H and B (High Leverage and Bracket order )
        /// </summary>
        [JsonProperty("trailprc")]
        public decimal TrailingPrice;
        public bool ShouldSerializeTrailingPrice()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (TrailingPrice > 0);
        }

        /// <summary>
        /// Yes
        /// The message "Invalid AMO" will be displayed if the "amo" field is not sent with a "Yes" value. If amo is not required, do not send this field.
        /// </summary>
        [JsonProperty("amo")]
        public string AMO;


        /// <summary>
        /// Trading symbol of second leg, mandatory for price type 2L and 3L (use url encoding to avoid special char error for symbols like M&M)
        /// </summary>
        [JsonProperty("tsym2")]
        public string TradingSymbol2;

        /// <summary>
        /// Transaction type of second leg, mandatory for price type 2L and 3L
        /// </summary>
        [JsonProperty("trantype2")]
        public string TransactionType2;

        /// <summary>
        /// 	Quantity for second leg, mandatory for price type 2L and 3L
        /// </summary>
        [JsonProperty("qty2")]
        public int Qty2;
        public bool ShouldSerializeQty2()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (Qty2 > 0);
        }

        /// <summary>
        /// 	Price for second leg, mandatory for price type 2L and 3L
        /// </summary>
        [JsonProperty("prc2")]
        public decimal Price2;
        public bool ShouldSerializePrice2()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (Price2 > 0);
        }

        /// <summary>
        /// Trading symbol of third leg, mandatory for price type 3L (use url encoding to avoid special char error for symbols like M&M)
        /// </summary>
        [JsonProperty("tsym3")]
        public string Transactionsymbol3;

        /// <summary>
        /// 	Transaction type of third leg, mandatory for price type 3L
        /// </summary>
        [JsonProperty("trantype3")]
        public string TransactionType3;

        /// <summary>
        /// 	Quantity for third leg, mandatory for price type 3L
        /// </summary>
        [JsonProperty("qty3")]
        public int Qty3;
        public bool ShouldSerializeQty3()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (Qty3 > 0);
        }

        /// <summary>
        /// Price for third leg, mandatory for price type 3L
        /// </summary>
        [JsonProperty("prc3")]
        public decimal Price3;
        public bool ShouldSerializePrice3()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (Price3 > 0);
        }

        /// <summary>
        /// 	market protection value in percentage
        /// </summary>
        [JsonProperty("mkt_protection")]
        public decimal MarketProtection;
        public bool ShouldSerializeMarketProtection()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (MarketProtection > 0);
        }


    }
}
