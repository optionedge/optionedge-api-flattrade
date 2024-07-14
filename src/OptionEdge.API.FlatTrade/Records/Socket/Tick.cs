using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;

namespace OptionEdge.API.FlatTrade.Records
{
    public class Tick
    {
        public Tick(dynamic data)
        {
            try
            {
                /// IMP:
                /// Inefficient handling of dynamic missing property check
                /// To be updated in coming versions
                /// https://stackoverflow.com/questions/2839598/how-to-detect-if-a-property-exists-on-an-expandoobject

                var responsType = data["t"];

                Exchange = data["e"];
                Token = Utils.ParseToInt(data["tk"]);

                if (responsType == Constants.SOCKET_RESPONSE_TYPE_TICK_ACKNOWLEDGEMENT)
                    TickType = TICK_TYPE.Tick_Ack;
                else if (responsType == Constants.SOCKET_RESPONSE_TYPE_TICK_DEPTH_ACKNOWLEDGEMENT)
                    TickType = TICK_TYPE.Tick_Depth_Ack;
                else if (responsType == Constants.SOCKET_RESPONSE_TYPE_TICK)
                    TickType = TICK_TYPE.Tick;
                else if (responsType == Constants.SOCKET_RESPONSE_TYPE_TICK_DEPTH)
                
                    
                    TickType = TICK_TYPE.Tick_Depth;


                PricePrecision = Utils.IsPropertyExist(data, "pp") ? Utils.ParseToInt(data["pp"]) : 2;
                TradingSymbol = Utils.IsPropertyExist(data, "ts") ? data["ts"] : null;
                TickSize = Utils.IsPropertyExist(data, "ti") ? Utils.ParseToDouble(data["ti"]) : 0;
                LotSize = Utils.IsPropertyExist(data, "ls") ? Utils.ParseToInt(data["ls"]) : 0;
                LTP = Utils.IsPropertyExist(data, "lp") ? Utils.ParseToDouble(data["lp"]) : 0;
                PercentageChange = Utils.IsPropertyExist(data, "pc") ? Utils.ParseToDouble(data["pc"]) : 0;
                Volume = Utils.IsPropertyExist(data, "v") ? Utils.ParseToInt(data["v"]) : 0;

                Open = Utils.IsPropertyExist(data, "o") ? Utils.ParseToDouble(data["o"]) : 0;
                High = Utils.IsPropertyExist(data, "h") ? Utils.ParseToDouble(data["h"]) : 0;
                Low = Utils.IsPropertyExist(data, "l") ? Utils.ParseToDouble(data["l"]) : 0;
                Close = Utils.IsPropertyExist(data, "c") ? Utils.ParseToDouble(data["c"]) : 0;
                AverageTradePrice = Utils.IsPropertyExist(data, "ap") ? Utils.ParseToDouble(data["ap"]) : 0;

                LastTradedTime = Utils.IsPropertyExist(data, "ltt") ? Utils.ParseToInt(data["ltt"]) : 0;

                LastTradedQty = Utils.IsPropertyExist(data, "ltq") ? Utils.ParseToInt(data["ltq"]) : 0;

                TotalBuyQty = Utils.IsPropertyExist(data, "tbq") ? Utils.ParseToInt(data["tbq"]) : 0;
                TotalSellQty = Utils.IsPropertyExist(data, "tsq") ? Utils.ParseToInt(data["tsq"]) : 0;


                BestBuyQty1 = Utils.IsPropertyExist(data, "bq1") ? Utils.ParseToInt(data["bq1"]) : 0;
                BestBuyQty2 = Utils.IsPropertyExist(data, "bq2") ? Utils.ParseToInt(data["bq2"]) : 0;
                BestBuyQty3 = Utils.IsPropertyExist(data, "bq3") ? Utils.ParseToInt(data["bq3"]) : 0;
                BestBuyQty4 = Utils.IsPropertyExist(data, "bq4") ? Utils.ParseToInt(data["bq4"]) : 0;
                BestBuyQty5 = Utils.IsPropertyExist(data, "bq5") ? Utils.ParseToInt(data["bq5"]) : 0;


                BestSellQty1 = Utils.IsPropertyExist(data, "sq1") ? Utils.ParseToInt(data["sq1"]) : 0;
                BestSellQty2 = Utils.IsPropertyExist(data, "sq2") ? Utils.ParseToInt(data["sq2"]) : 0;
                BestSellQty3 = Utils.IsPropertyExist(data, "sq3") ? Utils.ParseToInt(data["sq3"]) : 0;
                BestSellQty4 = Utils.IsPropertyExist(data, "sq4") ? Utils.ParseToInt(data["sq4"]) : 0;
                BestSellQty5 = Utils.IsPropertyExist(data, "sq5") ? Utils.ParseToInt(data["sq5"]) : 0;

                BestBuyPrice1 = Utils.IsPropertyExist(data, "bp1") ? Utils.ParseToDouble(data["bp1"]) : 0;
                BestBuyPrice2 = Utils.IsPropertyExist(data, "bp2") ? Utils.ParseToDouble(data["bp2"]) : 0;
                BestBuyPrice3 = Utils.IsPropertyExist(data, "bp3") ? Utils.ParseToDouble(data["bp3"]) : 0;
                BestBuyPrice4 = Utils.IsPropertyExist(data, "bp4") ? Utils.ParseToDouble(data["bp4"]) : 0;
                BestBuyPrice5 = Utils.IsPropertyExist(data, "bp5") ? Utils.ParseToDouble(data["bp5"]) : 0;

                BestSellPrice1 = Utils.IsPropertyExist(data, "sp1") ? Utils.ParseToDouble(data["sp1"]) : 0;
                BestSellPrice2 = Utils.IsPropertyExist(data, "sp2") ? Utils.ParseToDouble(data["sp2"]) : 0;
                BestSellPrice3 = Utils.IsPropertyExist(data, "sp3") ? Utils.ParseToDouble(data["sp3"]) : 0;
                BestSellPrice4 = Utils.IsPropertyExist(data, "sp4") ? Utils.ParseToDouble(data["sp4"]) : 0;
                BestSellPrice5 = Utils.IsPropertyExist(data, "sp5") ? Utils.ParseToDouble(data["sp5"]) : 0;

                UpperCircuit = Utils.IsPropertyExist(data, "uc") ? Utils.ParseToDouble(data["uc"]) : 0;
                LowerCircuit = Utils.IsPropertyExist(data, "lc") ? Utils.ParseToDouble(data["lc"]) : 0;

                FiftyTwo_52_WeeksHigh = Utils.IsPropertyExist(data, "52h") ? Utils.ParseToDouble(data["52h"]) : 0;
                FiftyTwo_52_WeeksLow = Utils.IsPropertyExist(data, "52l") ? Utils.ParseToDouble(data["52l"]) : 0;               
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public TICK_TYPE TickType { get; }
        public string Exchange { get;  }

        public int? Token { get;  }

        public string TradingSymbol { get; }
        public int LotSize { get; }

        public decimal TickSize { get; set; }

        public int PricePrecision { get; set; }

        public decimal? LTP { get; set; }

        public decimal? PercentageChange { get; set; }

        public int? Volume { get; set; }

        public decimal? Open { get; set; }

        public decimal? High { get; set; }

        public decimal? Low { get; set; }

        public decimal? Close { get; set; }

        public decimal? AverageTradePrice { get; set; }

        public decimal BestBuyPrice1 { get; set; }

        public decimal BestBuyQty1 { get; set; }

        public decimal BestSellPrice1 { get; set; }

        public decimal BestSellQty1 { get; set; }

        public int LastTradedQty { get; set; }

        public int TotalBuyQty { get; set; }
        public int TotalSellQty { get; set; }
        public int TotalOpenInterest { get; set; }


        public decimal BestBuyPrice2 { get; set; }

        public decimal BestBuyQty2 { get; set; }

        public decimal BestSellPrice2 { get; set; }

        public decimal BestSellQty2 { get; set; }



        public decimal BestBuyPrice3 { get; set; }

        public decimal BestBuyQty3 { get; set; }

        public decimal BestSellPrice3 { get; set; }

        public decimal BestSellQty3 { get; set; }



        public decimal BestBuyPrice4 { get; set; }

        public decimal BestBuyQty4 { get; set; }

        public decimal BestSellPrice4 { get; set; }

        public decimal BestSellQty4 { get; set; }



        public decimal BestBuyPrice5 { get; set; }

        public decimal BestBuyQty5 { get; set; }

        public decimal BestSellPrice5 { get; set; }

        public decimal BestSellQty5 { get; set; }

        public decimal UpperCircuit { get; set; }
        public decimal LowerCircuit { get; set; }

        public decimal FiftyTwo_52_WeeksHigh { get; set; }
        public decimal FiftyTwo_52_WeeksLow { get; set; }

        public long LastTradedTime { get; set; }
    }
}
