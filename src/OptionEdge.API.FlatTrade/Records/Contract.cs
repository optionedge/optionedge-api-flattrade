using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

namespace OptionEdge.API.FlatTrade.Records
{
    public class Contract
    {
        public string Exchange { get; set; }    

        public DateTime? Expiry { get; set; }

        public string Instrument { get; set; }
        

        public int LotSize { get; set; }

        public string OptionType { get; set; }

        public decimal Strike { get; set; }

        public string Symbol { get; set; }

        public decimal TickSize { get; set; }

        public int Token { get; set; }

        public string TradingSymbol { get; set; }

    }
}
