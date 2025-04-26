﻿using System;
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
    /// <summary>
    /// API client for FlatTrade broker
    /// </summary>
    public class FlatTradeApi : IDisposable
    {
        string _apiKey;
        string _apiSecret;
        string _userId;
        string _accountId;
        string _accessToken;

        bool _enableLogging;
        
        // Rate limiting configuration
        private readonly int _maxConcurrentRequests;
        private readonly SemaphoreSlim _throttler;
        private readonly Queue<TaskCompletionSource<bool>> _requestQueue;
        
        // Per-second rate limiting
        private int _requestsThisSecond;
        private readonly object _requestsPerSecondLock = new object();
        private System.Timers.Timer _requestsPerSecondTimer;

        protected readonly RestClient _restClient;

        public FlatTradeApi()
        {
            // Initialize with default values
            _maxConcurrentRequests = 5;
            _throttler = new SemaphoreSlim(_maxConcurrentRequests, _maxConcurrentRequests);
            _requestQueue = new Queue<TaskCompletionSource<bool>>();
        }

        public static FlatTradeApi CreateInstance(
            string userId,
            string accountId,
            string apiKey,
            string apiSecret,
            string baseUrlTrade = "",
            bool enableLogging = false,
            int maxConcurrentRequests = 5)
        {
            return new FlatTradeApi(
                userId,
                accountId,
                apiKey,
                apiSecret,
                baseUrlTrade,
                enableLogging,
                maxConcurrentRequests: maxConcurrentRequests);
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
            Func<string> cachedAccessTokenProvider = null,
            int maxConcurrentRequests = 30)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId), "User id required.");
            if (string.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey), "Api key required.");
            if (string.IsNullOrEmpty(apiSecret)) throw new ArgumentNullException(nameof(apiSecret), "Api secret required.");
            if (maxConcurrentRequests <= 0) {
                maxConcurrentRequests = 30;
                if (enableLogging) Utils.LogMessage("Max concurrent requests set to default value of 30.");
            }

            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _userId = userId;
            _accountId = accountId;

            _enableLogging = enableLogging;
            
            // Initialize rate limiting components
            _maxConcurrentRequests = maxConcurrentRequests;
            _throttler = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            _requestQueue = new Queue<TaskCompletionSource<bool>>();
            
            // Initialize per-second rate limiting
            _requestsThisSecond = 0;
            _requestsPerSecondTimer = new System.Timers.Timer(1000); // 1 second
            _requestsPerSecondTimer.Elapsed += (sender, e) => ResetRequestsPerSecond();
            _requestsPerSecondTimer.AutoReset = true;
            _requestsPerSecondTimer.Start();

            if (!string.IsNullOrEmpty(baseUrlTrade))
                _urls["baseUrlTrade"] = baseUrlTrade;

            var options = new RestClientOptions(_urls["baseUrlTrade"]);

            _restClient = new RestClient(options);
        }

        private Ticker _ticker;
        private bool _disposed = false;
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
        /// <summary>
        /// Gets the trading limits for the user
        /// </summary>
        /// <returns>The limits result containing available margins and limits</returns>
        public virtual async Task<LimitsResult> GetLimitsAsync()
        {
            return await ExecutePostAsync<LimitsResult>(_urls["limits"], GetBaseParams());
        }
        
        /// <summary>
        /// Gets the trading limits for the user (synchronous version)
        /// </summary>
        /// <returns>The limits result containing available margins and limits</returns>
        public virtual LimitsResult GetLimits()
        {
            return GetLimitsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets historical price data for a security
        /// </summary>
        /// <param name="historyDataParams">Parameters for the historical data request</param>
        /// <returns>Array of historical data points</returns>
        /// <exception cref="ArgumentNullException">Thrown when historyDataParams is null</exception>
        public virtual async Task<HistoryDataResult[]> GetHistoricalDataAsync(HistoryDataParams historyDataParams)
        {
            if (historyDataParams == null)
                throw new ArgumentNullException(nameof(historyDataParams), "History data parameters cannot be null");
                
            historyDataParams.UserId = _userId;
            historyDataParams.AccountId = _accountId;
            return await ExecutePostAsync<HistoryDataResult[]>(_urls["info.time.price.data"], historyDataParams);
        }
        
        /// <summary>
        /// Gets historical price data for a security (synchronous version)
        /// </summary>
        /// <param name="historyDataParams">Parameters for the historical data request</param>
        /// <returns>Array of historical data points</returns>
        /// <exception cref="ArgumentNullException">Thrown when historyDataParams is null</exception>
        public virtual HistoryDataResult[] GetHistoricalData(HistoryDataParams historyDataParams)
        {
            return GetHistoricalDataAsync(historyDataParams).GetAwaiter().GetResult();
        }


        /// <summary>
        /// Gets the history of a single order
        /// </summary>
        /// <param name="orderNumber">The order number to get history for</param>
        /// <returns>Array of order history results</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderNumber is null or empty</exception>
        public async Task<OrderHistoryResult[]> GetSingleOrderHistoryAsync(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber))
                throw new ArgumentNullException(nameof(orderNumber), "Order number cannot be null or empty");
                
            return await ExecutePostAsync<OrderHistoryResult[]>(_urls["single.order.history"], new OrderHistoryParams
            {
                UserId = _userId,
                OrderNumber = orderNumber
            });
        }
        
        /// <summary>
        /// Gets the history of a single order (synchronous version)
        /// </summary>
        /// <param name="orderNumber">The order number to get history for</param>
        /// <returns>Array of order history results</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderNumber is null or empty</exception>
        public OrderHistoryResult[] GetSingleOrderHistory(string orderNumber)
        {
            return GetSingleOrderHistoryAsync(orderNumber).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets a quote for a security
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <param name="token">The security token</param>
        /// <returns>Quote result</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchange or token is null or empty</exception>
        public async Task<GetQuoteResult> GetQuoteAsync(string exchange, string token)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentNullException(nameof(exchange), "Exchange cannot be null or empty");
                
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), "Token cannot be null or empty");
                
            return await ExecutePostAsync<GetQuoteResult>(_urls["get.quotes"], new GetQuoteParams
            {
                UserId = _userId,
                Exchange = exchange,
                Token = token
            });
        }
        
        /// <summary>
        /// Gets a quote for a security (synchronous version)
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <param name="token">The security token</param>
        /// <returns>Quote result</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchange or token is null or empty</exception>
        public GetQuoteResult GetQuote(string exchange, string token)
        {
            return GetQuoteAsync(exchange, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the history of a single order with retry logic
        /// </summary>
        /// <param name="orderNumber">The order number to get history for</param>
        /// <param name="hasOrderStatus">Predicate to check if the order has the desired status</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries in milliseconds</param>
        /// <returns>Order history result matching the status predicate</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderNumber is null or empty or hasOrderStatus is null</exception>
        public async Task<OrderHistoryResult> GetSingleOrderHistoryAsync(string orderNumber, Func<string, bool> hasOrderStatus, int maxRetries = 5, int retryDelay = 500)
        {
            if (string.IsNullOrEmpty(orderNumber))
                throw new ArgumentNullException(nameof(orderNumber), "Order number cannot be null or empty");
                
            if (hasOrderStatus == null)
                throw new ArgumentNullException(nameof(hasOrderStatus), "Order status predicate cannot be null");
                
            int retry = 1;
            OrderHistoryResult orderHistory = null;

            while (retry <= maxRetries)
            {
                retry++;

                var orderHistories = await GetSingleOrderHistoryAsync(orderNumber);
                if (orderHistories != null && orderHistories.Length > 0)
                {
                    orderHistory = orderHistories.Where(x => hasOrderStatus(x.OrderStatus)).FirstOrDefault();
                }

                if (orderHistory == null)
                {
                    await Task.Delay(retryDelay);
                    continue;
                }
                else
                    break;
            }

            return orderHistory;
        }
        
        /// <summary>
        /// Gets the history of a single order with retry logic (synchronous version)
        /// </summary>
        /// <param name="orderNumber">The order number to get history for</param>
        /// <param name="hasOrderStatus">Predicate to check if the order has the desired status</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryDelay">Delay between retries in milliseconds</param>
        /// <returns>Order history result matching the status predicate</returns>
        /// <exception cref="ArgumentNullException">Thrown when orderNumber is null or empty or hasOrderStatus is null</exception>
        public OrderHistoryResult GetSingleOrderHistory(string orderNumber, Func<string, bool> hasOrderStatus, int maxRetries = 5, int retryDelay = 500)
        {
            return GetSingleOrderHistoryAsync(orderNumber, hasOrderStatus, maxRetries, retryDelay).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Places an order with the specified parameters
        /// </summary>
        /// <param name="order">The order parameters</param>
        /// <returns>The result of the order placement</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing</exception>
        public virtual async Task<PlaceOrderResult> PlaceOrderAsync(PlaceOrderParams order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Order parameters cannot be null");
                
            order.UserId = _userId;
            order.AccountId = _accountId;

            PlaceOrderValidateRequiredArguments(order);

            if (string.IsNullOrEmpty(order.ProductCode))
                throw new ArgumentNullException(nameof(order.ProductCode), "Product code required.");

            if (string.IsNullOrEmpty(order.Exchange))
                order.Exchange = Constants.EXCHANGE_NFO;

            if (string.IsNullOrEmpty(order.RetentionType))
                order.RetentionType = Constants.RETENTION_TYPE_DAY;

            if (string.IsNullOrEmpty(order.PriceType))
                order.PriceType = Constants.PRICE_TYPE_LIMIT;

            order.OrderSource = "API";

            return await ExecutePostAsync<PlaceOrderResult>(_urls["order.place"], order);
        }
        
        /// <summary>
        /// Places an order with the specified parameters (synchronous version)
        /// </summary>
        /// <param name="order">The order parameters</param>
        /// <returns>The result of the order placement</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing</exception>
        public virtual PlaceOrderResult PlaceOrder(PlaceOrderParams order)
        {
            return PlaceOrderAsync(order).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Validates the required arguments for placing an order
        /// </summary>
        /// <param name="order">The order parameters to validate</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing</exception>
        protected virtual void PlaceOrderValidateRequiredArguments(PlaceOrderParams order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order), "Order parameters cannot be null");
                
            if (string.IsNullOrEmpty(order.TradingSymbol))
                throw new ArgumentNullException(nameof(order.TradingSymbol), "Trading symbol required.");

            if (order.InstrumentToken == 0)
                throw new ArgumentNullException(nameof(order.InstrumentToken), "Instrument token required.");

            //if (order.Quantity == 0)
            //    throw new ArgumentNullException(nameof(order.Quantity), "Quantity required.");

            if (string.IsNullOrEmpty(order.TransactionType))
                throw new ArgumentNullException(nameof(order.TransactionType), "Transaction type required.");

            if (string.IsNullOrEmpty(order.PriceType))
                throw new ArgumentNullException(nameof(order.PriceType), "Price type required.");
        }

        /// <summary>
        /// Downloads and saves the master contracts for the specified exchange
        /// </summary>
        /// <param name="exchange">The exchange code (e.g., NSE, NFO)</param>
        /// <param name="filePath">The file path to save the contracts to</param>
        /// <exception cref="DirectoryNotFoundException">File directory should exists.</exception>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing</exception>
        public virtual async Task SaveMasterContractsAsync(string exchange, string filePath)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentNullException(nameof(exchange), "Exchange cannot be null or empty");
                
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");
                
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Directory does not exist: {directory}");
                
            if (exchange == Constants.EXCHANGE_NFO || exchange == Constants.EXCHANGE_BFO)
            {
                await SaveMasterContractsInternalAsync(exchange + "_EQUITY", filePath);
                await SaveMasterContractsInternalAsync(exchange + "_INDEX", filePath);
            }
            else
            {
                await SaveMasterContractsInternalAsync(exchange, filePath);
            }
        }
        
        /// <summary>
        /// Downloads and saves the master contracts for the specified exchange (synchronous version)
        /// </summary>
        /// <param name="exchange">The exchange code (e.g., NSE, NFO)</param>
        /// <param name="filePath">The file path to save the contracts to</param>
        /// <exception cref="DirectoryNotFoundException">File directory should exists.</exception>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are missing</exception>
        public virtual void SaveMasterContracts(string exchange, string filePath)
        {
            SaveMasterContractsAsync(exchange, filePath).GetAwaiter().GetResult();
        }

        private async Task SaveMasterContractsInternalAsync(string exchange, string filePath)
        {
            await DownloadMasterContractAsync(exchange, async (stream) =>
            {
                using (var fileStream = File.Create(filePath))
                {
                    await stream.CopyToAsync(fileStream);
                }
            });
        }
        
        private void SaveMasterContractsInternal(string exchange, string filePath)
        {
            SaveMasterContractsInternalAsync(exchange, filePath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Downloads the master contract for the specified exchange
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <param name="processStream">Action to process the downloaded stream</param>
        /// <exception cref="ArgumentNullException">Thrown when exchange or processStream is null</exception>
        /// <summary>
        /// Downloads the master contract for the specified exchange asynchronously
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <param name="processStream">Action to process the downloaded stream</param>
        /// <exception cref="ArgumentNullException">Thrown when exchange or processStream is null</exception>
        protected async Task DownloadMasterContractAsync(string exchange, Func<Stream, Task> processStream)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentNullException(nameof(exchange), "Exchange cannot be null or empty");
                
            if (processStream == null)
                throw new ArgumentNullException(nameof(processStream), "Process stream action cannot be null");
                
            if (!_urlsContractMaster.ContainsKey(exchange))
            {
                if (_enableLogging)
                    Utils.LogMessage($"No contract master URL found for exchange: {exchange}");
                return;
            }

            var url = _urlsContractMaster[exchange];
            
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.Timeout = new TimeSpan(0, 0, 30);
                httpClient.DefaultRequestHeaders.Clear();

                using (var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        await processStream(stream);
                    }
                }
            }
        }
        
        /// <summary>
        /// Downloads the master contract for the specified exchange (synchronous version)
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <param name="processStream">Action to process the downloaded stream</param>
        /// <exception cref="ArgumentNullException">Thrown when exchange or processStream is null</exception>
        protected void DownloadMasterContract(string exchange, Action<Stream> processStream)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentNullException(nameof(exchange), "Exchange cannot be null or empty");
                
            if (processStream == null)
                throw new ArgumentNullException(nameof(processStream), "Process stream action cannot be null");
                
            DownloadMasterContractAsync(exchange, stream =>
            {
                processStream(stream);
                return Task.CompletedTask;
            }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the master contracts for the specified exchange
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <returns>List of contracts</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchange is null or empty</exception>
        public async Task<List<Contract>> GetMasterContractsAsync(string exchange)
        {
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentNullException(nameof(exchange), "Exchange cannot be null or empty");
                
            List<Contract> contracts = new List<Contract>(100000);

            if (exchange == Constants.EXCHANGE_NFO || exchange == Constants.EXCHANGE_BFO)
            {
                contracts.AddRange(await GetMasterContractsInternalAsync(exchange + "_EQUITY"));
                contracts.AddRange(await GetMasterContractsInternalAsync(exchange + "_INDEX"));
            }
            else
            {
                contracts.AddRange(await GetMasterContractsInternalAsync(exchange));
            }
            
            return contracts;
        }
        
        /// <summary>
        /// Gets the master contracts for the specified exchange (synchronous version)
        /// </summary>
        /// <param name="exchange">The exchange code</param>
        /// <returns>List of contracts</returns>
        /// <exception cref="ArgumentNullException">Thrown when exchange is null or empty</exception>
        public List<Contract> GetMasterContracts(string exchange)
        {
            return GetMasterContractsAsync(exchange).GetAwaiter().GetResult();
        }

        private async Task<List<Contract>> GetMasterContractsInternalAsync(string exchange)
        {
            List<Contract> contracts = new List<Contract>(999999);
            
            await DownloadMasterContractAsync(exchange, async (stream) =>
            {
                using (var streamReader = new StreamReader(stream))
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
        
        /// <summary>
        /// Gets the margin required for a basket of orders
        /// </summary>
        /// <param name="basketMarginParams">Basket margin parameters</param>
        /// <returns>Basket margin result</returns>
        /// <exception cref="ArgumentNullException">Thrown when basketMarginParams is null</exception>
        public async Task<BasketMarginResult> GetBasketMarginAsync(BasketMarginParams basketMarginParams)
        {
            if (basketMarginParams == null)
                throw new ArgumentNullException(nameof(basketMarginParams), "Basket margin parameters cannot be null");
                
            basketMarginParams.UserId = _userId;
            basketMarginParams.AccountId = _accountId;

            return await ExecutePostAsync<BasketMarginResult>(_urls["basket.margin"], basketMarginParams);
        }
        
        /// <summary>
        /// Gets the margin required for a basket of orders (synchronous version)
        /// </summary>
        /// <param name="basketMarginParams">Basket margin parameters</param>
        /// <returns>Basket margin result</returns>
        /// <exception cref="ArgumentNullException">Thrown when basketMarginParams is null</exception>
        public BasketMarginResult GetBasketMargin(BasketMarginParams basketMarginParams)
        {
            return GetBasketMarginAsync(basketMarginParams).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a POST request to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <returns>The response</returns>
        public T ExecutePost<T>(string endpoint, object inputParams = null) where T : class
        {
            return Execute<T>(endpoint, inputParams, Method.Post);
        }
        
        /// <summary>
        /// Executes a GET request to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <returns>The response</returns>
        public T ExecuteGet<T>(string endpoint, object inputParams = null) where T : class
        {
            return Execute<T>(endpoint, inputParams, Method.Get);
        }
        
        /// <summary>
        /// Executes a POST request asynchronously to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <returns>The response</returns>
        public async Task<T> ExecutePostAsync<T>(string endpoint, object inputParams = null) where T : class
        {
            return await ExecuteAsync<T>(endpoint, inputParams, Method.Post);
        }
        
        /// <summary>
        /// Executes a GET request asynchronously to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <returns>The response</returns>
        public async Task<T> ExecuteGetAsync<T>(string endpoint, object inputParams = null) where T : class
        {
            return await ExecuteAsync<T>(endpoint, inputParams, Method.Get);
        }

        /// <summary>
        /// Executes a request to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <param name="method">The HTTP method</param>
        /// <returns>The response</returns>
        protected T Execute<T>(string endpoint, object inputParams = null, Method method = Method.Get) where T : class
        {
            try
            {
                var request = new RestRequest(endpoint);

                if (inputParams != null)
                {
                    request.AddBody(ToJson(inputParams) + "&" + GetKey());
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

                    // Create a new instance of T with Status and ErrorMessage set
                    // instead of returning default(T)
                    if (typeof(BaseResponseResult).IsAssignableFrom(typeof(T)))
                    {
                        try
                        {
                            // Create a new instance of T
                            var result = Activator.CreateInstance<T>();
                            
                            // Set the Status and ErrorMessage properties
                            if (result is BaseResponseResult baseResult)
                            {
                                baseResult.Status = Constants.STATUS_NOT_OK;
                                baseResult.ErrorMessage = response.ErrorMessage ?? errorMessage;
                            }
                            
                            return result;
                        }
                        catch (Exception ex)
                        {
                            if (_enableLogging)
                                Utils.LogMessage($"Error creating instance of {typeof(T).Name}: {ex.Message}");
                        }
                    }

                    return default(T);
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    Utils.LogMessage($"Exception in Execute: {ex.Message}");
                    
                throw;
            }
        }

        /// <summary>
        /// Executes a request asynchronously to the specified endpoint
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="endpoint">The endpoint to call</param>
        /// <param name="inputParams">The input parameters</param>
        /// <param name="method">The HTTP method</param>
        /// <returns>The response</returns>
        protected async Task<T> ExecuteAsync<T>(string endpoint, object inputParams = null, Method method = Method.Get) where T : class
        {
            // Check if we've reached the per-second limit
            bool waitForNextSecond = false;
            
            lock (_requestsPerSecondLock)
            {
                if (_requestsThisSecond >= _maxConcurrentRequests)
                {
                    waitForNextSecond = true;
                    
                    if (_enableLogging)
                        Utils.LogMessage($"Rate limit reached for this second. Waiting for next second.");
                }
                else
                {
                    _requestsThisSecond++;
                    
                    if (_enableLogging)
                        Utils.LogMessage($"Request count for this second: {_requestsThisSecond}/{_maxConcurrentRequests}");
                }
            }
            
            if (waitForNextSecond)
            {
                // Wait until the next second
                await Task.Delay(1000);
                
                // Recursively call this method to try again
                return await ExecuteAsync<T>(endpoint, inputParams, method);
            }
            
            // Try to acquire a semaphore slot immediately
            bool acquired = _throttler.Wait(0);
            
            if (!acquired)
            {
                // If no slot is available, queue the request
                var tcs = new TaskCompletionSource<bool>();
                
                lock (_requestQueue)
                {
                    _requestQueue.Enqueue(tcs);
                    
                    if (_enableLogging)
                        Utils.LogMessage($"Request queued. Current queue size: {_requestQueue.Count}");
                }
                
                // Wait for a signal that a slot is available
                await tcs.Task;
            }
            
            try
            {
                var request = new RestRequest(endpoint);

                if (inputParams != null)
                {
                    request.AddBody(ToJson(inputParams) + "&" + GetKey());
                }

                var response = await _restClient.ExecuteAsync<T>(request, method);
                
                // Process the next queued request if any
                ProcessNextQueuedRequest();

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

                    // Create a new instance of T with Status and ErrorMessage set
                    // instead of returning default(T)
                    if (typeof(BaseResponseResult).IsAssignableFrom(typeof(T)))
                    {
                        try
                        {
                            // Create a new instance of T
                            var result = Activator.CreateInstance<T>();
                            
                            // Set the Status and ErrorMessage properties
                            if (result is BaseResponseResult baseResult)
                            {
                                baseResult.Status = Constants.STATUS_NOT_OK;
                                baseResult.ErrorMessage = response.ErrorMessage ?? errorMessage;
                            }
                            
                            return result;
                        }
                        catch (Exception ex)
                        {
                            if (_enableLogging)
                                Utils.LogMessage($"Error creating instance of {typeof(T).Name}: {ex.Message}");
                        }
                    }

                    return default(T);
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    Utils.LogMessage($"Exception in ExecuteAsync: {ex.Message}");
                    
                throw;
            }
            finally
            {
                // Release the semaphore slot if an exception occurs
                if (_throttler.CurrentCount == 0 && _requestQueue.Count == 0)
                {
                    _throttler.Release();
                }
            }
        }
        
        /// <summary>
        /// Processes the next request in the queue if any
        /// </summary>
        private void ProcessNextQueuedRequest()
        {
            lock (_requestQueue)
            {
                if (_requestQueue.Count > 0)
                {
                    // Get the next request from the queue
                    var nextRequest = _requestQueue.Dequeue();
                    
                    // Complete the task to signal that it can proceed
                    nextRequest.SetResult(true);
                }
                else
                {
                    // If no requests are queued, release the semaphore
                    _throttler.Release();
                }
            }
        }
        
        /// <summary>
        /// Resets the per-second request counter
        /// </summary>
        private void ResetRequestsPerSecond()
        {
            lock (_requestsPerSecondLock)
            {
                if (_enableLogging && _requestsThisSecond > 0)
                    Utils.LogMessage($"Resetting request counter. Processed {_requestsThisSecond} requests in the last second.");
                
                _requestsThisSecond = 0;
            }
        }

        /// <summary>
        /// Converts an object to JSON with the jData prefix
        /// </summary>
        /// <param name="data">The object to convert</param>
        /// <returns>JSON string with prefix</returns>
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

        /// <summary>
        /// Sets the access token for API requests
        /// </summary>
        /// <param name="accessToken">The access token</param>
        /// <exception cref="ArgumentNullException">Thrown when accessToken is null or empty</exception>
        public void SetAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken), "Access token cannot be null or empty");
                
            _accessToken = accessToken;
        }

        /// <summary>
        /// Refreshes the access token using the provided request code
        /// </summary>
        /// <param name="requestCode">The request code obtained from the authentication flow</param>
        /// <returns>The response containing the new access token or error information</returns>
        /// <exception cref="ArgumentNullException">Thrown when requestCode is null or empty</exception>
        public async Task<RefreshTokenResponse> RefreshAccessToken(string requestCode)
        {
            if (string.IsNullOrEmpty(requestCode))
                throw new ArgumentNullException(nameof(requestCode), "Request code cannot be null or empty");
                
            var response = new RefreshTokenResponse
            {
                Status = Constants.STATUS_NOT_OK
            };

            var options = new RestClientOptions(_urls["auth.token.url"]);
            using (var restClient = new RestClient(options))
            {
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
                        return response;
                    }

                    _accessToken = apiTokenResult?.Token;

                    response.Status = Constants.STATUS_OK;
                    response.AccessToken = _accessToken;
                }
                catch (Exception ex)
                {
                    response.Status = Constants.STATUS_NOT_OK;
                    response.Message = $"Error getting FlatTrade access token, Error Message: {ex.Message}";
                    if (_enableLogging)
                        Utils.LogMessage($"Error refreshing access token: {ex.Message}");
                }
            }

            return response;
        }

        /// <summary>
        /// Disposes the resources used by the FlatTradeApi client
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// Disposes the resources used by the FlatTradeApi client
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _restClient?.Dispose();
                    _ticker?.Dispose();
                    _throttler?.Dispose();
                    _requestsPerSecondTimer?.Stop();
                    _requestsPerSecondTimer?.Dispose();
                }
                
                _disposed = true;
            }
        }
    }
}