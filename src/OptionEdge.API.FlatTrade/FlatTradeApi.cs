using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OptionEdge.API.FlatTrade.Records;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Serializers.Json;
using Contract = OptionEdge.API.FlatTrade.Records.Contract;

namespace OptionEdge.API.FlatTrade
{
    public class FlatTradeApi
    {
        string _apiKey;
        string _apiSecret;
        string _userId;
        string _accountId;
        string _accessToken;

        bool _enableLogging;

        protected readonly RestClient _restClient;

        public FlatTradeApi()
        {

        }

        public static FlatTradeApi CreateInstance(
            string userId, 
            string accountId, 
            string apiKey, 
            string apiSecret, 
            string baseUrlTrade = "", 
            bool enableLogging = false)
        {
            return new FlatTradeApi(
                userId, 
                accountId, 
                apiKey,
                apiSecret,
                baseUrlTrade, 
                enableLogging);  
        }

        readonly Dictionary<string, string> _urls = new Dictionary<string, string>
        {
            // Urls
            ["auth.token.url"] = "https://authapi.flattrade.in/trade/apitoken",
            ["auth.request_code.url"] = "https://auth.flattrade.in/?app_key=APIKEY",
            ["baseUrlTrade"] = "https://piconnect.flattrade.in/PiConnectTP",
            ["websocketUrl"] = "wss://piconnect.flattrade.in/PiConnectWSTp/",

            ["order.place"] = "/PlaceOrder",
            ["order.modify"] = "/ModifyOrder",
            ["order.cancel"] = "/CancelOrder",
            ["order.exit.sno"] = "/ExitSNOOrder",
            ["basket.margin"] = "/GetBasketMargin ",
            ["order.book"] = "/OrderBook",
            ["multileg.order.book"] = "/MultiLegOrderBook",
            ["single.order.history"] = "/SingleOrdHist",
            ["trade.book"] = "/TradeBook",
            ["position.book"] = "/PositionBook",
            ["product.conversion"] = "/ProductConversion",

            ["modify.gtt.order"] = "/ModifyGTTOrder",
            ["pending.gtt.order"] = "/GetPendingGTTOrder",
            ["enabled.gtts"] = "/GetEnabledGTTs",
            ["modify.oco.order"] = "/ModifyOCOOrder",
            ["cancel.oco.order"] = "/CancelOCOOrder",

            ["holdings"] = "/Holdings",
            ["limits"] = "/Limits",

            ["info.index.list"] = "/GetIndexList",
            ["info.top.list.names"] = "/TopListName",
            ["info.top.list"] = "/TopList",
            ["info.time.price.data"] = "/TPSeries",
            ["info.eod.chart.data"] = "/EODChartData",
            ["info.option.greek"] = "/GetOptionGreek",
            ["info.broker.message"] = "/GetBrokerMsg",
            ["info.span.calculator"] = "/SpanCalc",

            ["alerts.set"] = "/SetAlert",
            ["alerts.modify"] = "/ModifyAlert",
            ["alerts.cancel"] = "/CancelAlert",
            ["alerts.get.pending"] = "/GetPendingAlert",
            ["unsettled.trading.date"] = "/GetUnStledTradingDate",


            ["funds.max.payout.amount"] = "/GetMaxPayoutAmount",
            ["funds.payout.request"] = "/FundsPayOutReq",
            ["funds.payin.report"] = "/GetPayinReport",
            ["funds.payout.report"] = "/FundsPayOutReq",
            ["funds.payout.cancel"] = "/CancelPayout",

            ["user.details"] = "/UserDetails",
            ["search.scrip"] = "/SearchScrip",
            ["get.quotes"] = "/GetQuotes",
        };

        readonly Dictionary<string, string> _urlsContractMaster = new Dictionary<string, string>
        {
            [Constants.EXCHANGE_NSE] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/NSE_Equity.csv",
            [Constants.EXCHANGE_NFO + "_EQUITY"] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Nfo_Equity_Derivatives.csv",
            [Constants.EXCHANGE_NFO + "_INDEX"] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Nfo_Index_Derivatives.csv",
            [Constants.EXCHANGE_NCO] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Currency_Derivatives.csv",
            [Constants.EXCHANGE_MCX] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Commodity.csv",
            [Constants.EXCHANGE_BSE] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/BSE_Equity.csv",
            [Constants.EXCHANGE_BFO + "_EQUITY"] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Bfo_Equity_Derivatives.csv",
            [Constants.EXCHANGE_BFO + "_INDEX"] = "https://flattrade.s3.ap-south-1.amazonaws.com/scripmaster/Bfo_Index_Derivatives.csv",
        };

        private FlatTradeApi (
            string userId, 
            string accountId, 
            string apiKey, 
            string apiSecret, 
            string baseUrlTrade = "", 
            bool enableLogging = false, 
            Action<string> onAccessTokenGenerated = null, 
            Func<string> cachedAccessTokenProvider = null)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException("User id required.");
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException("Api key required.");
            if (string.IsNullOrEmpty(apiSecret)) throw new ArgumentNullException("Api secret required.");

            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _userId = userId;
            _accountId = accountId;

            _enableLogging = enableLogging;

            if (!string.IsNullOrEmpty(baseUrlTrade))
                _urls["baseUrlTrade"] = baseUrlTrade;

            var options = new RestClientOptions(_urls["baseUrlTrade"]);

            _restClient = new RestClient(options);
        }

        private Ticker _ticker;
        public virtual Ticker CreateTicker(ILogger logger = null)
        {
            // Only single ticker instance allowed
            if (_ticker != null) return _ticker;

            _ticker = new Ticker(_userId, _accessToken, socketUrl: _urls["websocketUrl"], debug: _enableLogging, logger: logger);

            return _ticker;
        }       

        private BaseParams GetBaseParams()
        {
            return new BaseParams
            {
                AccountId = _userId,
                UserId = _userId
            };
        }
        public virtual LimitsResult GetLimits()
        {
            return ExecutePost<LimitsResult>(_urls["limits"], GetBaseParams());
        }      

        public virtual HistoryDataResult GetHistoricalData(HistoryDataParams historyDataParams)
        {
            var historicalDataBaseUrl = "https://a3.FlatTradeonline.com/rest/FlatTradeAPIService/chart/history";

            HistoryDataResult result = null;

            using (var restClient = new RestClient(historicalDataBaseUrl))
            {
                var request = new RestRequest();
                request.Method = Method.Get;
                request.AddQueryParameter("symbol", historyDataParams.InstrumentToken);
                request.AddQueryParameter("from", historyDataParams.From);
                request.AddQueryParameter("to", historyDataParams.To);
                request.AddQueryParameter("resolution", historyDataParams.Interval);
                request.AddQueryParameter("user", _userId);

                if (historyDataParams.Index)
                {
                    request.AddQueryParameter("exchange", $"{historyDataParams.Exchange}::index");
                }
                else
                {
                    request.AddQueryParameter("exchange", historyDataParams.Exchange);
                }

                var response = restClient.ExecuteGet<HistoryDataResult>(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK && response.Data != null)
                {
                    result = response.Data;

                    for (int i=0; i< response.Data.Close.Length; i++)
                    {
                        result.Candles.Add(new HistoryCandle
                        {
                            Open = response.Data.Open[i],
                            Close = response.Data.Close[i],
                            High = response.Data.High[i],   
                            Low = response.Data.Low[i],
                            Volume = response.Data.Volume[i],
                            IV  = response.Data.IV,
                            TimeData = response.Data.Time[i]
                        });
                    }
                }
            }
            
            return result;
        }

        public virtual HistoryDataResult GetHistoricalData(string exchange, int instrumentToken, DateTime from, DateTime to, string interval, bool index = false)
        {
            HistoryDataParams historyDataParams = new HistoryDataParams
            {
                Exchange = exchange,
                InstrumentToken = instrumentToken,
                From = ((DateTimeOffset)from).ToUnixTimeSeconds(),
                To = ((DateTimeOffset)to).ToUnixTimeSeconds(),
                Interval = interval,
                Index = index
            };

            return GetHistoricalData(historyDataParams);
        }

        public OrderHistoryResult[] GetSingleOrderHistory(string orderNumber)
        {            
            return ExecutePost<OrderHistoryResult[]>(_urls["single.order.history"], new OrderHistoryParams
            {
                UserId = _userId,
                OrderNumber = orderNumber
            });
        }

        public GetQuoteResult GetQuote(string exchange, string token)
        {
            return ExecutePost<GetQuoteResult>(_urls["get.quotes"], new GetQuoteParams
            {
                UserId = _userId,
                Exchange = exchange,
                Token = token
            });
        }

        public  OrderHistoryResult GetSingleOrderHistory(string orderNumber, Func<string, bool> hasOrderStatus, int maxRetries = 5, int retryDelay = 500)
        {
            int retry = 1;

            OrderHistoryResult orderHistory = null;

            while (retry <= maxRetries)
            {
                retry++;

                var orderHistories = GetSingleOrderHistory(orderNumber);
                if (orderHistories != null && orderHistories.Length > 0)
                {
                    orderHistory = orderHistories.Where(x => hasOrderStatus(x.OrderStatus)).FirstOrDefault();
                }

                if (orderHistory == null)
                {
                    Task.Delay(retryDelay).Wait();
                    continue;
                }
                else
                    break;
            }

            return orderHistory;
        }

        public virtual PlaceOrderResult PlaceOrder(PlaceOrderParams order)
        {
            order.UserId = _userId;
            order.AccountId = _accountId;

            PlaceOrderValidateRequiredArguments(order);

            if (string.IsNullOrEmpty(order.ProductCode))
                throw new ArgumentNullException("Product code required.");

            if (string.IsNullOrEmpty(order.Exchange))
                order.Exchange = Constants.EXCHANGE_NFO;

            if (string.IsNullOrEmpty(order.RetentionType))
                order.RetentionType = Constants.RETENTION_TYPE_DAY;

            if (string.IsNullOrEmpty(order.PriceType))
                order.PriceType = Constants.PRICE_TYPE_LIMIT;

            order.OrderSource = "API";

            return ExecutePost<PlaceOrderResult>(_urls["order.place"], order);
        }

        protected virtual void PlaceOrderValidateRequiredArguments(PlaceOrderParams order)
        {
            if (string.IsNullOrEmpty(order.TradingSymbol))
                throw new ArgumentNullException("Trading symbol required.");

            if (order.InstrumentToken == 0)
                throw new ArgumentNullException("Instrument token required.");

            //if (order.Quantity == 0)
            //    throw new ArgumentNullException("Quantity required.");

            if (string.IsNullOrEmpty(order.TransactionType))
                throw new ArgumentNullException("Transaction type required.");

            if (string.IsNullOrEmpty(order.PriceType))
                throw new ArgumentNullException("Price type required.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchange"></param>
        /// <param name="filePath"></param>
        /// <exception cref="DirectoryNotFoundException">File directory should exists.</exception>
        public virtual void SaveMasterContracts(string exchange, string filePath)
        {
            if (exchange == Constants.EXCHANGE_NFO || exchange == Constants.EXCHANGE_BFO)
            {
                SaveMasterContractsInternal(exchange + "_EQUITY", filePath);
                SaveMasterContractsInternal(exchange + "_INDEX", filePath);
            }

        }

        private void SaveMasterContractsInternal(string exchange, string filePath)
        {
            DownloadMasterContract(exchange, (stream) =>
            {
                var fileStream = File.Create(filePath);
                stream.CopyTo(fileStream);
                fileStream.Close();
            });
        }

        protected void DownloadMasterContract(string exchange, Action<Stream> processStream)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(0, 0, 30);
            httpClient.DefaultRequestHeaders.Clear();

            var url = _urlsContractMaster[exchange];

            using (var response = httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result)
            {
                response.EnsureSuccessStatusCode();

                var stream = response.Content.ReadAsStreamAsync().Result;
                using (stream)
                {
                    processStream.Invoke(stream);
                }
            }
        }

        public List<Contract> GetMasterContracts(string exchange)
        {
            List<Contract> contracts = new List<Contract>(100000);

            if (exchange == Constants.EXCHANGE_NFO || exchange == Constants.EXCHANGE_BFO)
            {
                contracts = GetMasterContractsInternal(exchange + "_EQUITY");

                contracts.AddRange( GetMasterContractsInternal(exchange + "_INDEX"));
            } else
            {
                contracts = GetMasterContractsInternal(exchange);

                contracts.AddRange(GetMasterContractsInternal(exchange));
            }
            
            return contracts;
        }

        private  List<Contract> GetMasterContractsInternal(string exchange)
        {
            List<Contract> contracts = new List<Contract>(999999);
            DownloadMasterContract(exchange, (stream) =>
            {
                var streamReader = new StreamReader(stream);
                using (var csv = new CsvReader(streamReader, true))
                {
                    while (csv.ReadNextRecord())
                    {
                        try
                        {
                            var contract = new Contract
                            {
                                Exchange = csv["Exchange"],
                                Token = int.Parse(csv["Token"]),
                                LotSize = csv.HasHeader("LotSize") && !string.IsNullOrEmpty(csv["LotSize"]) ? int.Parse(csv["LotSize"]) : 0,
                                Symbol = csv["Symbol"],
                                TradingSymbol = csv.HasHeader("TradingSymbol") ? csv["TradingSymbol"] : null,
                                Instrument = csv.HasHeader("Instrument") ? csv["Instrument"] : null,
                                Expiry = csv.HasHeader("Expiry") && !string.IsNullOrEmpty(csv["Expiry"]) ? DateTime.Parse(csv["Expiry"]) : default(DateTime?),
                                Strike = csv.HasHeader("Strike") && !string.IsNullOrEmpty(csv["Strike"]) ? decimal.Parse(csv["Strike"]) : 0.0m,
                                OptionType = csv.HasHeader("OptionType") ? csv["OptionType"] : null,
                            };

                            if (string.IsNullOrEmpty(contract.TradingSymbol))
                            {
                                contract.TradingSymbol = contract.Symbol;
                            }

                            contracts.Add(contract);
                        }
                        catch (Exception ex)
                        {
                            if (_enableLogging) Utils.LogMessage(ex.ToString());
                        }
                    }
                }
            });

            return contracts;
        }

        public T ExecutePost<T>(string endpoint, object inputParams = null) where T : class
        {
            return Execute<T>(endpoint, inputParams, Method.Post);
        }
        public T ExecuteGet<T>(string endpoint, object inputParams = null) where T : class
        {
            return Execute<T>(endpoint, inputParams, Method.Get);
        }

        protected T Execute<T>(string endpoint, object inputParams = null, Method method = Method.Get) where T : class
        {
            var request = new RestRequest(endpoint);

            if (inputParams != null)
            {
                request.AddBody(ToJson(inputParams) + "&" + GetKey());

                //request.AddParameter("jData", new StringContent( content).ReadAsStringAsync().Result, ParameterType.RequestBody);
                //request.AddParameter("jKey",_accessToken, ParameterType.RequestBody);
                ////request.AddQueryParameter("jKey", Utils.Serialize(_accessToken));
            }

            var response = _restClient.ExecuteAsync<T>(request, method).Result;

            if (response != null && !string.IsNullOrEmpty(response.ErrorMessage) && _enableLogging)
                Utils.LogMessage($"Error executing api request. Status: {response.StatusCode}-{response.ErrorMessage}");


            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return response.Data;
            else
            {
                var errorMessage = $@"
                        Status Code: {response.StatusCode}
                        Status Description: {response.StatusDescription}
                        Content: {response.Content}
                        Error Message: {response.ErrorMessage}
                        Error Exception: {response.ErrorException?.Message}";

                if (_enableLogging)
                    Utils.LogMessage(errorMessage);
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    throw new UnauthorizedAccessException(errorMessage);

                return default(T);
            }
        }

        private string ToJson(object data)
        {
            string json = JsonConvert.SerializeObject(data, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            string prefix = "jData=";
            return prefix + json;
        }

        private string GetKey()
        {
            return "jKey=" + _accessToken;
        }

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
        }

        public async Task<RefreshTokenResponse> RefreshAccessToken(string requestCode)
        {
            var response = new RefreshTokenResponse
            {
                Status = Constants.STATUS_NOT_OK
            };

            var options = new RestClientOptions(_urls["auth.token.url"]);
            var restClient = new RestClient(options);

            var request = new RestRequest();

            var apiSecretSHA256 = Utils.GetSHA256($"{_apiKey}{requestCode}{_apiSecret}");

            var apiTokenParams = new ApiTokenParams
            {
                ApiKey = _apiKey,
                RequestCode = requestCode,
                ApiSecret = apiSecretSHA256,
            };

            request.AddStringBody(JsonConvert.SerializeObject(apiTokenParams), ContentType.Json);

            try
            {
                var apiTokenResult = await restClient.PostAsync<APITokenResult>(request);

                if (apiTokenResult.Status == Constants.API_RESPONSE_STATUS_Not_OK)
                {
                    response.Status = Constants.STATUS_NOT_OK;
                    response.Message = $"Unable to get access token: {apiTokenResult.Status}, Error Message: {apiTokenResult.ErrorMessage}";
                }

                if (restClient != null) restClient.Dispose();

                _accessToken = apiTokenResult?.Token;

                response.Status = Constants.STATUS_OK;
                response.AccessToken = _accessToken;
            }
            catch (Exception ex)
            {
                response.Status = Constants.STATUS_NOT_OK;
                response.Message = $"Error getting FlatTrade access token, Error Message: {ex.Message}";
            }

            return response;
        }
    }
}